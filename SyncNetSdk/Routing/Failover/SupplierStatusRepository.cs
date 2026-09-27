using SyncNet.DbRepository;
using SyncNet.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncNet.Routing.Failover
{
    // state kesehatan supplier (ACTIVE/DOWN/SUSPECT) dipakai lintas strategi routing
    // dan lintas kategori (topup = MARGIN, bill payment = PRODUCT). status dilacak per
    // supplier (node), bukan per produk.
    //
    // health check fase 2 (DOCS/Routing/03-routing-dinamis/FASE-2-HEALTH-CHECK.md):
    //   link down  rc link down, INQUIRY+PAYMENT      -> langsung DOWN
    //   timeout    rc timeout N kali, PAYMENT          -> SUSPECT
    //   rc gagal   rc gagal N kali, INQUIRY+PAYMENT    -> SUSPECT
    //   pending    rc pending N kali, PAYMENT          -> SUSPECT
    //   latency    respons > ambang N kali, INQ+PAY    -> SUSPECT (walau rc sukses)
    // setiap kategori punya counter, max consecutive, dan cooldown sendiri. satu rc boleh
    // ada di beberapa daftar; semua kategori yang memuatnya ikut menghitung.
    public class SupplierStatusRepository
    {
        public const string ROUTING_MARGIN = "MARGIN";
        public const string ROUTING_PRODUCT = "PRODUCT";

        // reason failover log saat transaksi ditolak Jadwal Routing (Fase 3)
        public const string SCHEDULE_CLOSED = "SCHEDULE_CLOSED";

        // rc sukses: host biller (2 digit) dan core (4 digit)
        private static readonly HashSet<string> SuccessCodes = ["00", "0000"];

        // dipakai kalau belum ada baris config untuk routing type tsb. cooldown kosong
        // = hanya reset manual, diatur lewat Routing > Health Check Config.
        private static readonly FailoverModel.Config DefaultConfig = new()
        {
            InstId = null,
            RcLinkDown = "91,1091",
            RcSuspect = "68,1068",
            MaxConsecutiveSuspect = 3,
            LinkDownCooldownMinutes = null,
            SuspectCooldownMinutes = null,
            IsActive = true
        };

        private readonly ConcurrentDictionary<string, FailoverModel.SupplierStatus> _status = new();

        // snapshot config immutable: Initialize membangun yang baru lalu menukar referensi,
        // jadi pembaca tidak pernah melihat kondisi setengah terisi saat RESYNC.
        private volatile Dictionary<string, FailoverModel.Config> _config = [];

        private readonly ISupplierStatusStore _store;
        private readonly Func<DateTime> _clock;

        public SupplierStatusRepository() : this(new DbMgr(), () => DateTime.Now)
        {
        }

        public SupplierStatusRepository(ISupplierStatusStore store, Func<DateTime> clock)
        {
            _store = store;
            _clock = clock;
        }

        public static bool IsSuccessCode(string rcCode)
        {
            return rcCode != null && SuccessCodes.Contains(rcCode);
        }

        public async Task Initialize()
        {
            var status = await _store.GetSupplierStatus();
            var config = await _store.GetFailoverConfig();

            var newConfig = new Dictionary<string, FailoverModel.Config>();
            foreach (var c in config)
            {
                string key = ConfigKey(c.RoutingType, c.InstId);
                if (newConfig.ContainsKey(key) == false)
                    newConfig.Add(key, c);
            }

            _status.Clear();
            foreach (var s in status)
                _status[s.SupplierId] = s;

            _config = newConfig;
        }

        // saklar darurat per kategori: baris config global (inst_id kosong) yang nonaktif
        // mematikan failover untuk routing type itu. tanpa baris = failover aktif.
        public bool IsFailoverEnabled(string routingType)
        {
            if (_config.TryGetValue(ConfigKey(routingType, null), out var global) == true)
                return global.IsActive;

            return true;
        }

        // eligible = boleh dipilih untuk siklus baru. supplier yang belum pernah
        // bermasalah (tidak ada baris status) dianggap ACTIVE. supplier DOWN/SUSPECT
        // yang cooldown-nya sudah lewat dianggap eligible lagi - siklus berikutnya
        // yang dikirim ke situ otomatis jadi transaksi uji.
        public bool IsEligible(string supplierId)
        {
            if (_status.TryGetValue(supplierId, out var s) == false)
                return true;

            lock (s)
            {
                if (s.Status == FailoverModel.SupplierStatusEnum.ACTIVE)
                    return true;

                return s.BlockedUntil.HasValue == true && s.BlockedUntil.Value <= _clock();
            }
        }

        // status kini (salinan), untuk monitoring dan unit test
        public FailoverModel.SupplierStatus GetStatus(string supplierId)
        {
            if (_status.TryGetValue(supplierId, out var s) == false)
                return null;

            lock (s)
            {
                return Clone(s);
            }
        }

        // dipanggil untuk setiap respons INQUIRY/PAYMENT dari supplier hasil routing dinamis.
        // latencyMs = lama respons diukur di aplikasi channel (null = tidak diukur).
        public async Task RecordResponseAsync(string supplierId, string routingType, bool isPayment,
            string rcCode, int? latencyMs = null, string instId = null, long? denom = null,
            string traceNumber = null, string updatedBy = "System")
        {
            var config = ResolveConfig(routingType, instId);

            bool isSuccess = IsSuccessCode(rcCode);
            bool isLinkDown = isSuccess == false && config.IsLinkDownCode(rcCode);
            bool isTimeout = isSuccess == false && isPayment == true && config.IsSuspectCode(rcCode);
            bool isFailed = isSuccess == false && config.IsFailedCode(rcCode);
            bool isPending = isSuccess == false && isPayment == true && config.IsPendingCode(rcCode);

            //timeout tidak dihitung sebagai latency (respons dibuat core, bukan biller)
            bool latencyCounted = config.IsLatencyActive == true && latencyMs.HasValue == true &&
                config.IsSuspectCode(rcCode) == false && isLinkDown == false;
            bool isSlow = latencyCounted == true && latencyMs.Value > config.LatencyThresholdMs.Value;

            var s = _status.GetOrAdd(supplierId, id => new FailoverModel.SupplierStatus { SupplierId = id });

            FailoverModel.SupplierStatus snapshot = null;
            string reason = null;

            lock (s)
            {
                DateTime now = _clock();
                var before = Clone(s);

                bool blocked = s.Status != FailoverModel.SupplierStatusEnum.ACTIVE;
                bool inWindow = blocked == true && (s.BlockedUntil.HasValue == false || s.BlockedUntil.Value > now);
                bool probe = blocked == true && s.BlockedUntil.HasValue == true && s.BlockedUntil.Value <= now;

                if (latencyMs.HasValue == true)
                    s.LastLatencyMs = latencyMs;

                if (inWindow == true)
                {
                    //semua kandidat sedang diblokir sehingga transaksi tetap dikirim ke primary.
                    //kegagalan di sini tidak memperpanjang cooldown dan bukan transaksi uji;
                    //payment sukses yang cepat memulihkan supplier.
                    if (isPayment == true && isSuccess == true && isSlow == false)
                        SetActive(s);
                }
                else if (isLinkDown == true)
                {
                    reason = FailoverModel.BlockReason.LINK_DOWN;
                    Block(s, FailoverModel.SupplierStatusEnum.DOWN, reason, config, now, probe);
                }
                else
                {
                    //counter hanya direset oleh PAYMENT (sukses; untuk latency: respons cepat).
                    //INQUIRY boleh menambah counter tetapi tidak mereset: pada bill payment inquiry
                    //hampir selalu sukses & cepat sebelum payment, sehingga kalau ikut mereset,
                    //kegagalan/lambat berturut-turut di payment tidak pernah terkumpul.
                    if (isSuccess == true)
                    {
                        if (isPayment == true)
                        {
                            s.ConsecutiveFailedCount = 0;
                            s.ConsecutiveSuspectCount = 0;
                            s.ConsecutivePendingCount = 0;
                        }
                    }
                    else
                    {
                        if (isTimeout == true) s.ConsecutiveSuspectCount++;
                        if (isFailed == true) s.ConsecutiveFailedCount++;
                        if (isPending == true) s.ConsecutivePendingCount++;
                    }

                    if (isSlow == true)
                        s.ConsecutiveLatencyCount++;
                    else if (latencyCounted == true && isPayment == true)
                        s.ConsecutiveLatencyCount = 0;

                    if (probe == true)
                    {
                        //transaksi uji: satu kejadian kategori mana pun langsung memblokir lagi
                        reason = FirstReason(isTimeout, isPending, isFailed, isSlow);

                        if (reason != null)
                            Block(s, FailoverModel.SupplierStatusEnum.SUSPECT, reason, config, now, probe: true);
                        else if (isPayment == true && isSuccess == true)
                            SetActive(s);
                    }
                    else
                    {
                        reason = FirstReason(
                            isTimeout == true && s.ConsecutiveSuspectCount >= Math.Max(1, config.MaxConsecutiveSuspect),
                            isPending == true && s.ConsecutivePendingCount >= Math.Max(1, config.MaxConsecutivePending),
                            isFailed == true && s.ConsecutiveFailedCount >= Math.Max(1, config.MaxConsecutiveFailed),
                            isSlow == true && s.ConsecutiveLatencyCount >= Math.Max(1, config.MaxConsecutiveLatency));

                        if (reason != null)
                            Block(s, FailoverModel.SupplierStatusEnum.SUSPECT, reason, config, now, probe: false);
                    }
                }

                //tulis DB hanya bila status/counter berubah, bukan di setiap transaksi sukses
                if (HasStateChanged(before, s) == true)
                {
                    s.LastRcCode = rcCode;
                    s.LastTranDt = now;
                    s.UpdatedBy = updatedBy;
                    s.UpdatedDt = now;
                    snapshot = Clone(s);
                }
            }

            if (snapshot != null)
                await _store.UpsertSupplierStatus(snapshot);

            if (reason != null)
                await _store.InsertFailoverLog(routingType, instId, denom, traceNumber, supplierId, null, rcCode, reason,
                    latencyCounted == true ? latencyMs : null);
        }

        // transaksi ditolak karena Jadwal Routing (tidak ada biller buka). hanya dicatat di failover log
        // untuk audit (FASE-3 Q5), status kesehatan biller tidak berubah.
        public Task LogScheduleRejectAsync(string routingType, string instId, long? denom, string traceNumber,
            string supplierId, string rcCode, int? scheduleId)
        {
            return _store.InsertFailoverLog(routingType, instId, denom, traceNumber, supplierId, null, rcCode,
                SCHEDULE_CLOSED, null, scheduleId);
        }

        public async Task ResetManualAsync(string supplierId, string updatedBy)
        {
            var s = _status.GetOrAdd(supplierId, id => new FailoverModel.SupplierStatus { SupplierId = id });

            FailoverModel.SupplierStatus snapshot;

            lock (s)
            {
                SetActive(s);
                s.UpdatedBy = updatedBy;
                s.UpdatedDt = _clock();

                snapshot = Clone(s);
            }

            await _store.UpsertSupplierStatus(snapshot);
        }

        // urutan bila beberapa kategori terpicu bersamaan: timeout, pending, rc gagal, latency
        private static string FirstReason(bool timeout, bool pending, bool failed, bool latency)
        {
            if (timeout == true) return FailoverModel.BlockReason.TIMEOUT;
            if (pending == true) return FailoverModel.BlockReason.PENDING;
            if (failed == true) return FailoverModel.BlockReason.RC_FAILED;
            if (latency == true) return FailoverModel.BlockReason.LATENCY;
            return null;
        }

        private static void SetActive(FailoverModel.SupplierStatus s)
        {
            s.Status = FailoverModel.SupplierStatusEnum.ACTIVE;
            s.BlockReason = null;
            s.RetryCount = 0;
            s.BlockedSince = null;
            s.BlockedUntil = null;
            ResetCounters(s);
        }

        private static void ResetCounters(FailoverModel.SupplierStatus s)
        {
            s.ConsecutiveSuspectCount = 0;
            s.ConsecutiveFailedCount = 0;
            s.ConsecutivePendingCount = 0;
            s.ConsecutiveLatencyCount = 0;
        }

        // counter dimulai dari nol setelah blokir. probe: waktu mulai gangguan yang asli
        // dipertahankan dan retry_count bertambah. cooldown kosong = butuh reset manual.
        private static void Block(FailoverModel.SupplierStatus s, FailoverModel.SupplierStatusEnum status,
            string reason, FailoverModel.Config config, DateTime now, bool probe)
        {
            s.Status = status;
            s.BlockReason = reason;
            ResetCounters(s);

            if (probe == true) s.RetryCount++;

            if (probe == false || s.BlockedSince.HasValue == false)
                s.BlockedSince = now;

            int? cooldown = config.CooldownFor(reason);
            s.BlockedUntil = cooldown.HasValue == true ? now.AddMinutes(cooldown.Value) : null;
        }

        private static bool HasStateChanged(FailoverModel.SupplierStatus a, FailoverModel.SupplierStatus b)
        {
            return a.Status != b.Status
                || a.BlockReason != b.BlockReason
                || a.ConsecutiveSuspectCount != b.ConsecutiveSuspectCount
                || a.ConsecutiveFailedCount != b.ConsecutiveFailedCount
                || a.ConsecutivePendingCount != b.ConsecutivePendingCount
                || a.ConsecutiveLatencyCount != b.ConsecutiveLatencyCount
                || a.RetryCount != b.RetryCount
                || a.BlockedSince != b.BlockedSince
                || a.BlockedUntil != b.BlockedUntil;
        }

        // config per produk mengalahkan config global routing type-nya. baris nonaktif
        // dilewati. tanpa baris sama sekali dipakai default bawaan.
        private FailoverModel.Config ResolveConfig(string routingType, string instId)
        {
            var config = _config;

            if (string.IsNullOrEmpty(instId) == false &&
                config.TryGetValue(ConfigKey(routingType, instId), out var byProduct) == true &&
                byProduct.IsActive == true)
                return byProduct;

            if (config.TryGetValue(ConfigKey(routingType, null), out var global) == true &&
                global.IsActive == true)
                return global;

            return DefaultConfig;
        }

        private static string ConfigKey(string routingType, string instId)
        {
            return $"{routingType}|{instId}";
        }

        private static FailoverModel.SupplierStatus Clone(FailoverModel.SupplierStatus s)
        {
            return new FailoverModel.SupplierStatus
            {
                SupplierId = s.SupplierId,
                Status = s.Status,
                LastRcCode = s.LastRcCode,
                ConsecutiveSuspectCount = s.ConsecutiveSuspectCount,
                ConsecutiveFailedCount = s.ConsecutiveFailedCount,
                ConsecutivePendingCount = s.ConsecutivePendingCount,
                ConsecutiveLatencyCount = s.ConsecutiveLatencyCount,
                BlockReason = s.BlockReason,
                LastLatencyMs = s.LastLatencyMs,
                RetryCount = s.RetryCount,
                LastTranDt = s.LastTranDt,
                BlockedSince = s.BlockedSince,
                BlockedUntil = s.BlockedUntil,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt
            };
        }
    }
}
