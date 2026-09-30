using System.Collections.Concurrent;
using SyncNetPro.Contracts;

namespace SyncNetPro.Routing;

/// <summary>
/// Kesehatan supplier (ACTIVE/DOWN/SUSPECT) lintas strategi dan kategori — port 1:1 <c>SupplierStatusRepository</c> SDK lama.
/// <list type="bullet">
/// <item>link down (INQUIRY+PAYMENT) → langsung DOWN</item>
/// <item>timeout N kali (PAYMENT) → SUSPECT</item>
/// <item>rc gagal N kali (INQUIRY+PAYMENT) → SUSPECT</item>
/// <item>pending N kali (PAYMENT) → SUSPECT</item>
/// <item>latency di atas ambang N kali (INQUIRY+PAYMENT) → SUSPECT walau rc sukses</item>
/// </list>
/// Counter hanya direset PAYMENT sukses; supplier yang cooldown-nya lewat menjadi transaksi uji.
/// </summary>
public sealed class SupplierHealthTracker(ISupplierHealthStore store, TimeProvider time)
{
    private static readonly HashSet<string> SuccessCodes = ["00", "0000"];

    // Tanpa baris config: default bawaan (cooldown kosong = hanya reset manual).
    private static readonly FailoverConfig DefaultConfig = new()
    {
        RoutingType = string.Empty,
        RcLinkDown = "91,1091",
        RcTimeout = "68,1068",
        MaxConsecutiveTimeout = 3,
        IsActive = true,
    };

    private readonly ConcurrentDictionary<string, SupplierHealth> _status = new();
    private volatile Dictionary<string, FailoverConfig> _config = [];

    /// <summary>RC sukses biller (2 digit) atau Core (4 digit).</summary>
    public static bool IsSuccessCode(string? rc) => rc is not null && SuccessCodes.Contains(rc);

    /// <summary>Memuat status &amp; config (start dan RESYNC); config diganti utuh.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SupplierHealth> status = await store.GetSupplierHealthAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FailoverConfig> config = await store.GetFailoverConfigAsync(cancellationToken).ConfigureAwait(false);

        var map = new Dictionary<string, FailoverConfig>();
        foreach (FailoverConfig c in config) map.TryAdd(Key(c.RoutingType, c.InstId), c);

        _status.Clear();
        foreach (SupplierHealth s in status) _status[s.SupplierId] = s;
        _config = map;
    }

    /// <summary>Saklar darurat: baris config global kategori yang nonaktif mematikan failover; tanpa baris = aktif.</summary>
    public bool IsFailoverEnabled(string routingType) =>
        !_config.TryGetValue(Key(routingType, null), out FailoverConfig? global) || global.IsActive;

    /// <summary>Boleh dipilih untuk siklus baru (tanpa riwayat = ACTIVE; cooldown lewat = eligible sebagai transaksi uji).</summary>
    public bool IsEligible(string supplierId)
    {
        if (!_status.TryGetValue(supplierId, out SupplierHealth? s)) return true;
        lock (s)
        {
            return s.Status == SupplierHealthStatus.Active || (s.BlockedUntil is DateTime until && until <= Now);
        }
    }

    /// <summary>Salinan status kini (monitoring).</summary>
    public SupplierHealth? GetStatus(string supplierId)
    {
        if (!_status.TryGetValue(supplierId, out SupplierHealth? s)) return null;
        lock (s) return s.Clone();
    }

    /// <summary>Mencatat respons INQUIRY/PAYMENT dari supplier hasil routing dinamis.</summary>
    /// <param name="supplierId">Supplier.</param>
    /// <param name="routingType">Kategori.</param>
    /// <param name="isPayment">PAYMENT (selain itu INQUIRY).</param>
    /// <param name="rcCode">RC respons.</param>
    /// <param name="latencyMs">Lama respons diukur aplikasi channel.</param>
    /// <param name="instId">Produk (config per produk).</param>
    /// <param name="denom">Denom (failover log).</param>
    /// <param name="traceNumber">Trace (failover log).</param>
    /// <param name="updatedBy">Pengubah.</param>
    /// <param name="cancellationToken">Pembatalan.</param>
    public async Task RecordResponseAsync(string supplierId, string routingType, bool isPayment, string? rcCode, int? latencyMs = null,
        string? instId = null, long? denom = null, string? traceNumber = null, string updatedBy = "System", CancellationToken cancellationToken = default)
    {
        FailoverConfig config = ResolveConfig(routingType, instId);

        bool isSuccess = IsSuccessCode(rcCode);
        bool isLinkDown = !isSuccess && config.IsLinkDownCode(rcCode);
        bool isTimeout = !isSuccess && isPayment && config.IsTimeoutCode(rcCode);
        bool isFailed = !isSuccess && config.IsFailedCode(rcCode);
        bool isPending = !isSuccess && isPayment && config.IsPendingCode(rcCode);

        // Timeout tidak dihitung sebagai latency (respons dibuat Core, bukan biller).
        bool latencyCounted = config.IsLatencyActive && latencyMs.HasValue && !config.IsTimeoutCode(rcCode) && !isLinkDown;
        bool isSlow = latencyCounted && latencyMs!.Value > config.LatencyThresholdMs!.Value;

        SupplierHealth s = _status.GetOrAdd(supplierId, id => new SupplierHealth { SupplierId = id });
        SupplierHealth? snapshot = null;
        string? reason = null;

        lock (s)
        {
            DateTime now = Now;
            SupplierHealth before = s.Clone();

            bool blocked = s.Status != SupplierHealthStatus.Active;
            bool inWindow = blocked && (s.BlockedUntil is null || s.BlockedUntil.Value > now);
            bool probe = blocked && s.BlockedUntil is DateTime until && until <= now;

            if (latencyMs.HasValue) s.LastLatencyMs = latencyMs;

            if (inWindow)
            {
                // Semua kandidat diblokir sehingga transaksi tetap ke primary: kegagalan tidak memperpanjang cooldown,
                // payment sukses yang cepat memulihkan supplier.
                if (isPayment && isSuccess && !isSlow) SetActive(s);
            }
            else if (isLinkDown)
            {
                reason = BlockReasons.LinkDown;
                Block(s, SupplierHealthStatus.Down, reason, config, now, probe);
            }
            else
            {
                // Counter hanya direset PAYMENT: inquiry bill payment hampir selalu sukses sebelum payment.
                if (isSuccess)
                {
                    if (isPayment)
                    {
                        s.ConsecutiveFailedCount = 0;
                        s.ConsecutiveTimeoutCount = 0;
                        s.ConsecutivePendingCount = 0;
                    }
                }
                else
                {
                    if (isTimeout) s.ConsecutiveTimeoutCount++;
                    if (isFailed) s.ConsecutiveFailedCount++;
                    if (isPending) s.ConsecutivePendingCount++;
                }

                if (isSlow) s.ConsecutiveLatencyCount++;
                else if (latencyCounted && isPayment) s.ConsecutiveLatencyCount = 0;

                if (probe)
                {
                    // Transaksi uji: satu kejadian kategori mana pun langsung memblokir lagi.
                    reason = FirstReason(isTimeout, isPending, isFailed, isSlow);
                    if (reason is not null) Block(s, SupplierHealthStatus.Suspect, reason, config, now, probe: true);
                    else if (isPayment && isSuccess) SetActive(s);
                }
                else
                {
                    reason = FirstReason(
                        isTimeout && s.ConsecutiveTimeoutCount >= Math.Max(1, config.MaxConsecutiveTimeout),
                        isPending && s.ConsecutivePendingCount >= Math.Max(1, config.MaxConsecutivePending),
                        isFailed && s.ConsecutiveFailedCount >= Math.Max(1, config.MaxConsecutiveFailed),
                        isSlow && s.ConsecutiveLatencyCount >= Math.Max(1, config.MaxConsecutiveLatency));
                    if (reason is not null) Block(s, SupplierHealthStatus.Suspect, reason, config, now, probe: false);
                }
            }

            // Tulis DB hanya bila status/counter berubah.
            if (!before.SameState(s))
            {
                s.LastRcCode = rcCode;
                s.LastTransactionAt = now;
                s.UpdatedBy = updatedBy;
                s.UpdatedAt = now;
                snapshot = s.Clone();
            }
        }

        if (snapshot is not null) await store.UpsertSupplierHealthAsync(snapshot, cancellationToken).ConfigureAwait(false);

        if (reason is not null)
        {
            await store.InsertFailoverLogAsync(
                new FailoverLogEntry(routingType, instId, denom, traceNumber, supplierId, null, rcCode, reason, latencyCounted ? latencyMs : null, null, Now),
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Transaksi ditolak Jadwal Routing: hanya failover log (audit), status tidak berubah.</summary>
    public Task LogScheduleRejectAsync(string routingType, string? instId, long? denom, string? traceNumber, string? supplierId, string rcCode,
        int? scheduleId, CancellationToken cancellationToken = default) =>
        store.InsertFailoverLogAsync(new FailoverLogEntry(routingType, instId, denom, traceNumber, supplierId, null, rcCode, BlockReasons.ScheduleClosed, null, scheduleId, Now), cancellationToken);

    /// <summary>Reset manual ke ACTIVE.</summary>
    public async Task ResetAsync(string supplierId, string updatedBy, CancellationToken cancellationToken = default)
    {
        SupplierHealth s = _status.GetOrAdd(supplierId, id => new SupplierHealth { SupplierId = id });
        SupplierHealth snapshot;
        lock (s)
        {
            SetActive(s);
            s.UpdatedBy = updatedBy;
            s.UpdatedAt = Now;
            snapshot = s.Clone();
        }

        await store.UpsertSupplierHealthAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private DateTime Now => time.GetLocalNow().DateTime;

    // Urutan bila beberapa kategori terpicu bersamaan: timeout, pending, rc gagal, latency.
    private static string? FirstReason(bool timeout, bool pending, bool failed, bool latency) =>
        timeout ? BlockReasons.Timeout : pending ? BlockReasons.Pending : failed ? BlockReasons.RcFailed : latency ? BlockReasons.Latency : null;

    private static void SetActive(SupplierHealth s)
    {
        s.Status = SupplierHealthStatus.Active;
        s.BlockReason = null;
        s.RetryCount = 0;
        s.BlockedSince = null;
        s.BlockedUntil = null;
        ResetCounters(s);
    }

    private static void ResetCounters(SupplierHealth s)
    {
        s.ConsecutiveTimeoutCount = 0;
        s.ConsecutiveFailedCount = 0;
        s.ConsecutivePendingCount = 0;
        s.ConsecutiveLatencyCount = 0;
    }

    // Probe: waktu mulai gangguan asli dipertahankan dan retry bertambah. Cooldown kosong = reset manual.
    private static void Block(SupplierHealth s, SupplierHealthStatus status, string reason, FailoverConfig config, DateTime now, bool probe)
    {
        s.Status = status;
        s.BlockReason = reason;
        ResetCounters(s);
        if (probe) s.RetryCount++;
        if (!probe || s.BlockedSince is null) s.BlockedSince = now;
        int? cooldown = config.CooldownFor(reason);
        s.BlockedUntil = cooldown.HasValue ? now.AddMinutes(cooldown.Value) : null;
    }

    // Config per produk mengalahkan config global; baris nonaktif dilewati; tanpa baris = default.
    private FailoverConfig ResolveConfig(string routingType, string? instId)
    {
        Dictionary<string, FailoverConfig> config = _config;
        if (!string.IsNullOrEmpty(instId) && config.TryGetValue(Key(routingType, instId), out FailoverConfig? byProduct) && byProduct.IsActive) return byProduct;
        if (config.TryGetValue(Key(routingType, null), out FailoverConfig? global) && global.IsActive) return global;
        return DefaultConfig;
    }

    private static string Key(string routingType, string? instId) => $"{routingType}|{instId}";
}

/// <summary>Kunci korelasi Core untuk siklus transaksi (<c>DataHelper.GetSwitchKey</c>/<c>GetSwitchKeyOrig</c> Core).</summary>
public static class SwitchKeys
{
    /// <summary><c>UPPER(tran_type + datetime_tran + trace_number + terminal_id)</c>.</summary>
    public static string Build(string? tranType, string? transactionDateTime, string? traceNumber, string? terminalId) =>
        string.Concat(tranType, transactionDateTime, traceNumber, terminalId).ToUpperInvariant();

    /// <summary>Kunci PAYMENT asal pada ADVICE/REVERSAL: <c>UPPER(original_data + terminal_id)</c>.</summary>
    public static string BuildOriginal(string? originalData, string? terminalId) =>
        string.Concat(originalData, terminalId).ToUpperInvariant();

    /// <summary>
    /// Tanggal transaksi dari <c>yyyyMMddHHmmss</c> di awal <c>datetime_tran</c>, atau setelah tran type (3 huruf) pada
    /// <c>original_data</c>; <c>null</c> bila tidak sesuai format (mis. datetime 10 digit) — pemanggil memakai hari ini.
    /// </summary>
    public static DateTime? ParseTransactionDate(string? value, bool hasTranTypePrefix = false)
    {
        if (string.IsNullOrEmpty(value)) return null;
        int start = hasTranTypePrefix ? 3 : 0;
        if (value.Length < start + 14) return null;
        return DateTime.TryParseExact(value.AsSpan(start, 14), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out DateTime d) ? d : null;
    }

    internal static bool IsCycleOpener(string? tranType) => tranType is TranType.Inquiry or TranType.Payment;
}
