using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Models;
using SyncNet.Routing.Failover;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Routing.Commitment
{
    // sumber config, aturan, volume, dan log Volume & Tiering. implementasi produksi = DbMgr; unit test
    // memakai store di memori.
    public interface ICommitmentStore
    {
        // saklar ON/OFF global (sw_routes_commitment_config); tanpa baris/tabel = OFF
        Task<bool> IsCommitmentActive();

        Task<List<CommitmentRuleModel>> GetCommitmentRules();

        // total per biller + produk (hari ini, kemarin, bulan ini, bulan lalu relatif `today`) untuk biller
        // yang punya aturan
        Task<List<VolumeTotalModel>> GetVolumeTotals(DateTime today, IReadOnlyCollection<string> nodeNames);

        // tambahkan delta ke sw_routes_volume (UPSERT tran_count = tran_count + delta) dalam satu
        // transaksi; exception = tidak ada yang tersimpan
        Task AddVolumes(IReadOnlyList<VolumeModel> deltas);

        // event baru; yang sudah ada (aturan + periode + event + nilai sama) diabaikan
        Task InsertCommitmentLogs(IReadOnlyList<CommitmentLogModel> logs);
    }

    // Volume & Tiering (Level 3, FASE-5-KOMITMEN-BISNIS.md §4, §5, revisi 2 C19–C27).
    //
    // ON/OFF (C19): dibaca saat Initialize (start dan RESYNC / Terapkan Perubahan). OFF = tidak ada
    // pencatatan, timer flush, query volume, maupun L3.
    //
    // volume (tanggal = tanggal PAY asal, C25):
    //   PAYMENT sukses              +1
    //   ADVICE sukses (cek status)  +1 bila PAY asal belum dihitung (PAY suspect); ADV berulang tidak menambah (C21)
    //   REVERSAL sukses             -1 bila PAY asal dihitung instance ini (C22)
    // kunci PAY asal = switch key PAYMENT (tran_type + datetime + trace + terminal) = original_data + terminal.
    // delta disimpan di memori lalu ditulis ke sw_routes_volume setiap FlushSeconds (UPSERT atomik, aman untuk
    // banyak instance API Channel); setelah itu total biller beraturan dimuat ulang sebagai agregat dan di-cache
    // per aturan (C27). volume = total cache + delta lokal, jadi kuota bisa terlewati sedikit (C7). selisih
    // (crash, reversal/advice di instance lain) dibetulkan rekonsiliasi H-1 job CleanTrans (C23).
    //
    //   Kuota (LIMIT)        : biller dibuang bila volume periode ini >= limit; semua habis = kuota diabaikan (C14)
    //   Target Tier (TARGET) : biller didahulukan selama volume periode ini < target
    // event ditulis ke sw_routes_commitment_log saat flush, sekali per aturan + periode + event + nilai.
    public class CommitmentRepository
    {
        // ADV/REV dicocokkan dengan PAY yang dihitung dalam rentang ini
        private static readonly TimeSpan CountedRetention = TimeSpan.FromDays(2);

        private volatile bool _active;
        private volatile List<CommitmentRuleModel> _rules = [];
        private volatile HashSet<string> _nodes = [];
        private volatile RuleTotals _totals = RuleTotals.Empty;

        private readonly object _sync = new();
        private Dictionary<VolumeKey, VolumeModel> _pending = [];
        private List<VolumeModel> _inFlight = [];
        private readonly Dictionary<string, CountedPayment> _counted = [];
        private readonly HashSet<string> _loggedKeys = [];
        private readonly ConcurrentQueue<CommitmentLogModel> _logQueue = new();
        private readonly SemaphoreSlim _flushLock = new(1, 1);
        private DateTime? _lastMissedCheck;
        private Timer _timer;

        private readonly ICommitmentStore _store;
        private readonly Func<DateTime> _clock;
        private readonly int _flushSeconds;

        private readonly record struct VolumeKey(DateTime Date, string RoutingType, string NodeName, string InstId);
        private sealed record CountedPayment(VolumeKey Key, decimal Amount, DateTime At);

        // total DB per aturan saat dimuat: periode yang memuat LoadedAt dan periode sebelumnya
        private sealed record RuleTotals(DateTime LoadedAt, Dictionary<int, decimal> Current, Dictionary<int, decimal> Previous)
        {
            public static readonly RuleTotals Empty = new(DateTime.MinValue, [], []);
        }

        public CommitmentRepository(int flushSeconds = 5) : this(new DbMgr(), () => DateTime.Now, flushSeconds)
        {
        }

        // flushSeconds <= 0 = tanpa timer (unit test memanggil FlushAsync sendiri)
        public CommitmentRepository(ICommitmentStore store, Func<DateTime> clock, int flushSeconds)
        {
            _store = store;
            _clock = clock;
            _flushSeconds = flushSeconds;
        }

        public bool IsActive => _active;
        public bool HasRules => _active == true && _rules.Count > 0;

        public async Task Initialize()
        {
            await _flushLock.WaitAsync();
            try
            {
                //delta yang belum tersimpan ditulis dulu supaya tidak hilang saat RESYNC / dimatikan
                try
                {
                    await FlushCoreAsync();
                }
                catch (Exception ex)
                {
                    await LogAsync($"Volume & Tiering flush: {ex.Message}");
                }

                //gagal memuat (mis. tabel belum dibuat) tidak boleh menghentikan start/RESYNC: fitur dianggap OFF
                bool active = false;
                List<CommitmentRuleModel> rules = [];
                try
                {
                    active = await _store.IsCommitmentActive();
                    if (active == true)
                        rules = await _store.GetCommitmentRules();
                }
                catch (Exception ex)
                {
                    active = false;
                    await LogAsync($"Volume & Tiering initialize: {ex.Message}");
                }

                _rules = rules;
                _nodes = rules.Where(r => string.IsNullOrEmpty(r.NodeName) == false).Select(r => r.NodeName).ToHashSet();
                _active = active;

                if (active == false)
                {
                    lock (_sync)
                    {
                        _pending = [];
                        _inFlight = [];
                        _counted.Clear();
                    }
                    _totals = RuleTotals.Empty;
                }
                else
                {
                    try
                    {
                        await ReloadTotalsAsync();
                    }
                    catch (Exception ex)
                    {
                        await LogAsync($"Volume & Tiering load volume: {ex.Message}");
                    }
                }
            }
            finally
            {
                _flushLock.Release();
            }

            //timer hanya berjalan saat ON (C19: OFF tanpa beban)
            if (_active == true && _timer == null && _flushSeconds > 0)
            {
                _timer = new Timer(_ => _ = FlushSafeAsync(), null, _flushSeconds * 1000, _flushSeconds * 1000);
            }
            else if (_active == false && _timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        // ---------------------------------------------------------------- pencatatan volume

        // dipanggil untuk setiap respons dari biller. hanya PAYMENT, ADVICE, dan REVERSAL sukses yang berpengaruh.
        // switchKey = kunci PAYMENT ini; originalSwitchKey = kunci PAYMENT asal pada ADVICE/REVERSAL.
        // tranDate = tanggal PAYMENT (asal); null = sekarang.
        public void RecordVolume(string routingType, string nodeName, string productId, string tranType,
            string rcCode, decimal amount, string switchKey = null, string originalSwitchKey = null, DateTime? tranDate = null)
        {
            if (_active == false) return;
            if (string.IsNullOrEmpty(nodeName) == true || string.IsNullOrEmpty(productId) == true) return;
            if (SupplierStatusRepository.IsSuccessCode(rcCode) == false) return;

            var now = _clock();
            var date = SaneDate(tranDate, now);

            lock (_sync)
            {
                switch (tranType)
                {
                    case TranType.PAYMENT:
                        if (Count(switchKey, new VolumeKey(date, routingType, nodeName, productId), amount, now) == false) return;
                        break;

                    case TranType.ADVICE:
                        //cek status PAY suspect: dihitung sekali dari PAY asal
                        if (string.IsNullOrEmpty(originalSwitchKey) == true) return;
                        if (Count(originalSwitchKey, new VolumeKey(date, routingType, nodeName, productId), amount, now) == false) return;
                        break;

                    case TranType.REVERSAL:
                        //PAY yang tidak dihitung di sini (gagal/suspect, instance lain) tidak dikurangi;
                        //selisihnya dibetulkan rekonsiliasi harian
                        if (string.IsNullOrEmpty(originalSwitchKey) == true || _counted.Remove(originalSwitchKey, out var paid) == false)
                            return;
                        AddPending(paid.Key, -1, -paid.Amount);
                        return;

                    default:
                        return;
                }
            }

            if (_nodes.Contains(nodeName) == true)
                CheckThresholds(_rules.Where(r => r.InScope(routingType, nodeName, productId) == true), now);
        }

        // tambah satu transaksi; false = PAY ini sudah dihitung (respons ganda / ADV berulang)
        private bool Count(string key, VolumeKey volumeKey, decimal amount, DateTime now)
        {
            if (string.IsNullOrEmpty(key) == false)
            {
                if (_counted.ContainsKey(key) == true) return false;
                _counted[key] = new CountedPayment(volumeKey, amount, now);
            }
            AddPending(volumeKey, 1, amount);
            return true;
        }

        // tanggal transaksi dari aplikasi channel; jam CA yang meleset jauh (> 2 hari) diabaikan
        private static DateTime SaneDate(DateTime? tranDate, DateTime now) =>
            tranDate.HasValue == true && Math.Abs((tranDate.Value - now).TotalDays) <= 2 ? tranDate.Value.Date : now.Date;

        // "yyyyMMddHHmmss" di awal datetime_tran, atau setelah tran_type (3 huruf) pada original_data
        public static DateTime? ParseTranDate(string value, bool hasTranTypePrefix = false)
        {
            if (string.IsNullOrEmpty(value) == true) return null;
            int start = hasTranTypePrefix == true ? 3 : 0;
            if (value.Length < start + 14) return null;

            return DateTime.TryParseExact(value.Substring(start, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : null;
        }

        private void AddPending(VolumeKey key, long count, decimal amount)
        {
            if (_pending.TryGetValue(key, out var v) == false)
            {
                v = new VolumeModel { Date = key.Date, RoutingType = key.RoutingType, NodeName = key.NodeName, InstId = key.InstId };
                _pending[key] = v;
            }
            v.Count += count;
            v.Amount += amount;
        }

        // volume aturan pada periode yang memuat `day` (sampai `day`): total cache DB + delta lokal
        public decimal Volume(CommitmentRuleModel rule, DateTime day)
        {
            var from = rule.PeriodStart(day);
            var to = day.Date;
            decimal sum = StoredTotal(rule, from);

            lock (_sync)
            {
                sum += Sum(_pending.Values, rule, from, to);
                sum += Sum(_inFlight, rule, from, to);
            }

            return sum;
        }

        // total DB aturan untuk periode yang dimulai `periodStart`: periode saat dimuat atau periode sebelumnya;
        // periode lain (mis. hari/bulan baru sebelum flush berikutnya) = 0
        private decimal StoredTotal(CommitmentRuleModel rule, DateTime periodStart)
        {
            var totals = _totals;
            if (totals.LoadedAt == DateTime.MinValue) return 0;

            var current = rule.PeriodStart(totals.LoadedAt);
            if (periodStart == current)
                return totals.Current.TryGetValue(rule.Id, out var c) ? c : 0;
            if (periodStart == rule.PeriodStart(current.AddDays(-1)))
                return totals.Previous.TryGetValue(rule.Id, out var p) ? p : 0;
            return 0;
        }

        private static decimal Sum(IEnumerable<VolumeModel> rows, CommitmentRuleModel rule, DateTime from, DateTime to)
        {
            decimal sum = 0;
            foreach (var v in rows)
            {
                if (v.NodeName != rule.NodeName || v.RoutingType != rule.RoutingType) continue;
                if (rule.InstId != null && v.InstId != rule.InstId) continue;
                if (v.Date < from || v.Date > to) continue;
                sum += rule.Metric == CommitmentRuleModel.COUNT ? v.Count : v.Amount;
            }
            return sum;
        }

        // ---------------------------------------------------------------- evaluasi L3

        // terapkan Volume & Tiering pada kandidat hasil Jadwal Routing (sudah urut: Prioritas Jadwal di
        // depan, lalu urutan Routing Mode). buang biller yang kuotanya habis (semua habis = kuota
        // diabaikan), lalu biller yang mengejar target dipindah ke depan setelah Prioritas Jadwal (stabil).
        public CommitmentOrder<T> Apply<T>(IReadOnlyList<T> ordered, Func<T, string> nodeName, Func<T, bool> isScheduled,
            string routingType, string productId)
        {
            var list = ordered.ToList();
            if (_active == false || list.Count == 0)
                return new CommitmentOrder<T>(list, [], false);

            var now = _clock();
            var relevant = _rules.Where(r => r.RoutingType == routingType && (r.InstId == null || r.InstId == productId)
                && r.IsValidOn(now) == true).ToList();
            if (relevant.Count == 0)
                return new CommitmentOrder<T>(list, [], false);

            var limitedBy = new Dictionary<string, List<(CommitmentRuleModel Rule, decimal Volume)>>();
            var targeted = new HashSet<string>();

            foreach (var node in list.Select(nodeName).Distinct())
            {
                foreach (var r in relevant.Where(r => r.NodeName == node))
                {
                    decimal v = Volume(r, now);
                    if (r.RuleType == CommitmentRuleModel.LIMIT && v >= r.Threshold)
                    {
                        if (limitedBy.TryGetValue(node, out var by) == false)
                            limitedBy[node] = by = [];
                        by.Add((r, v));
                    }
                    else if (r.RuleType == CommitmentRuleModel.TARGET && v < r.Threshold)
                    {
                        targeted.Add(node);
                    }
                }
            }

            var open = list.Where(x => limitedBy.ContainsKey(nodeName(x)) == false).ToList();
            bool allLimited = open.Count == 0;
            if (allLimited == true)
            {
                //C14: semua kandidat habis kuotanya -> kuota diabaikan, transaksi tidak ditolak
                open = list;
                foreach (var (r, v) in limitedBy.Values.SelectMany(x => x))
                    QueueLog(r, CommitmentLogModel.LIMIT_IGNORED, v, r.PeriodStart(now), productId, now);
            }

            var result = open
                .Select((x, i) => (Item: x, Rank: isScheduled(x) == true ? 0 : targeted.Contains(nodeName(x)) == true ? 1 : 2, Index: i))
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Index)
                .Select(x => x.Item)
                .ToList();

            return new CommitmentOrder<T>(result, targeted, allLimited);
        }

        // ---------------------------------------------------------------- event

        private void CheckThresholds(IEnumerable<CommitmentRuleModel> rules, DateTime now)
        {
            foreach (var r in rules)
            {
                if (r.IsValidOn(now) == false) continue;

                decimal v = Volume(r, now);
                var period = r.PeriodStart(now);

                if (r.RuleType == CommitmentRuleModel.LIMIT)
                {
                    decimal warnAt = r.Threshold * (r.WarnPct ?? 80) / 100m;
                    if (v >= warnAt) QueueLog(r, CommitmentLogModel.LIMIT_WARN, v, period, r.InstId, now);
                    if (v >= r.Threshold) QueueLog(r, CommitmentLogModel.LIMIT_REACHED, v, period, r.InstId, now);
                }
                else if (v >= r.Threshold)
                {
                    QueueLog(r, CommitmentLogModel.TARGET_REACHED, v, period, r.InstId, now);
                }
            }
        }

        // target periode sebelumnya yang tidak tercapai, diperiksa sekali per hari (flush pertama hari itu).
        // hanya aturan yang sudah ada sebelum periode ini dimulai dan berlaku di hari terakhir periode lalu.
        private void CheckMissedTargets(DateTime now)
        {
            if (_lastMissedCheck == now.Date) return;
            _lastMissedCheck = now.Date;

            lock (_sync)
            {
                //kunci log periode lama tidak dibutuhkan lagi; duplikat dicegah unique di DB
                _loggedKeys.Clear();
            }

            foreach (var r in _rules.Where(r => r.RuleType == CommitmentRuleModel.TARGET))
            {
                var current = r.PeriodStart(now);
                var lastDay = current.AddDays(-1);
                if (r.IsValidOn(lastDay) == false) continue;
                if (r.CreatedDt.HasValue == true && r.CreatedDt.Value >= current) continue;

                decimal v = Volume(r, lastDay);
                if (v < r.Threshold)
                    QueueLog(r, CommitmentLogModel.TARGET_MISSED, v, r.PeriodStart(lastDay), r.InstId, now);
            }
        }

        private void QueueLog(CommitmentRuleModel r, string evt, decimal volume, DateTime periodStart, string instId, DateTime now)
        {
            string key = $"{r.Id}|{periodStart:yyyyMMdd}|{evt}|{r.Threshold}";
            lock (_sync)
            {
                if (_loggedKeys.Add(key) == false) return;
            }

            _logQueue.Enqueue(new CommitmentLogModel
            {
                CommitmentId = r.Id,
                RuleType = r.RuleType,
                RoutingType = r.RoutingType,
                NodeName = r.NodeName,
                InstId = instId,
                Metric = r.Metric,
                PeriodType = r.PeriodType,
                PeriodStart = periodStart,
                Event = evt,
                VolumeValue = volume,
                ThresholdValue = r.Threshold,
                CreatedDt = now
            });
        }

        // ---------------------------------------------------------------- flush

        private async Task FlushSafeAsync()
        {
            try
            {
                await FlushAsync();
            }
            catch (Exception ex)
            {
                await LogAsync($"Volume & Tiering flush: {ex.Message}");
            }
        }

        // tulis delta ke DB, muat ulang total biller yang punya aturan, periksa event, tulis log.
        // flush yang sedang berjalan tidak ditunggu (timer berikutnya mengulang).
        public async Task FlushAsync()
        {
            if (_active == false) return;
            if (await _flushLock.WaitAsync(0) == false) return;

            try
            {
                await FlushCoreAsync();
            }
            finally
            {
                _flushLock.Release();
            }
        }

        // dipanggil dengan _flushLock dipegang
        private async Task FlushCoreAsync()
        {
            var now = _clock();
            List<VolumeModel> deltas;
            List<VolumeModel> unreloaded;
            lock (_sync)
            {
                deltas = _pending.Values.Where(v => v.Count != 0 || v.Amount != 0).ToList();
                _pending = [];
                //delta yang sudah ditulis tetapi belum masuk cache (reload sebelumnya gagal) tetap dihitung
                unreloaded = _inFlight;
                _inFlight = [.. unreloaded, .. deltas];

                foreach (var k in _counted.Where(c => now - c.Value.At > CountedRetention).Select(c => c.Key).ToList())
                    _counted.Remove(k);
            }

            if (deltas.Count > 0)
            {
                try
                {
                    await _store.AddVolumes(deltas);
                }
                catch
                {
                    //gagal simpan: delta dikembalikan dan dicoba lagi pada flush berikutnya
                    lock (_sync)
                    {
                        foreach (var d in deltas)
                            AddPending(new VolumeKey(d.Date, d.RoutingType, d.NodeName, d.InstId), d.Count, d.Amount);
                        _inFlight = unreloaded;
                    }
                    throw;
                }
            }

            if (_active == false)
            {
                lock (_sync) _inFlight = [];
                return;
            }

            await ReloadTotalsAsync();

            if (_rules.Count > 0)
            {
                CheckMissedTargets(now);
                CheckThresholds(_rules, now);
            }

            await WriteLogsAsync();
        }

        // agregat per biller + produk dari DB, dijumlahkan per aturan (C27): evaluasi per transaksi hanya
        // membaca satu angka per aturan
        private async Task ReloadTotalsAsync()
        {
            var nodes = _nodes;
            var rules = _rules;
            var today = _clock().Date;

            var current = new Dictionary<int, decimal>();
            var previous = new Dictionary<int, decimal>();

            if (nodes.Count > 0 && rules.Count > 0)
            {
                var totals = await _store.GetVolumeTotals(today, nodes);
                foreach (var r in rules)
                {
                    decimal cur = 0, prev = 0;
                    foreach (var t in totals)
                    {
                        if (t.NodeName != r.NodeName || t.RoutingType != r.RoutingType) continue;
                        if (r.InstId != null && t.InstId != r.InstId) continue;

                        bool count = r.Metric == CommitmentRuleModel.COUNT;
                        if (r.PeriodType == CommitmentRuleModel.MONTHLY)
                        {
                            cur += count ? t.MonthCount : t.MonthAmount;
                            prev += count ? t.PrevMonthCount : t.PrevMonthAmount;
                        }
                        else
                        {
                            cur += count ? t.TodayCount : t.TodayAmount;
                            prev += count ? t.YesterdayCount : t.YesterdayAmount;
                        }
                    }
                    current[r.Id] = cur;
                    previous[r.Id] = prev;
                }
            }

            lock (_sync)
            {
                _totals = new RuleTotals(today, current, previous);
                _inFlight = [];
            }
        }

        private async Task WriteLogsAsync()
        {
            var logs = new List<CommitmentLogModel>();
            while (_logQueue.TryDequeue(out var log))
                logs.Add(log);
            if (logs.Count == 0) return;

            try
            {
                await _store.InsertCommitmentLogs(logs);
            }
            catch (Exception ex)
            {
                await LogAsync($"Volume & Tiering log: {ex.Message}");
            }
        }

        private static async Task LogAsync(string message)
        {
            try
            {
                await AppProcessor.Logger(message);
            }
            catch
            {
                //logger tidak tersedia (mis. unit test)
            }
        }
    }

    // hasil penerapan Volume & Tiering. Targeted = biller yang sedang mengejar target (didahulukan);
    // AllLimited = semua kandidat habis kuotanya dan kuota diabaikan.
    public sealed record CommitmentOrder<T>(List<T> Open, HashSet<string> Targeted, bool AllLimited);
}
