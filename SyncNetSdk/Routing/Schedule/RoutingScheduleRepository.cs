using SyncNet.DbRepository;
using SyncNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.Routing.Schedule
{
    // sumber aturan jadwal. implementasi produksi = DbMgr; unit test memakai daftar di memori.
    public interface IRoutingScheduleStore
    {
        Task<List<RoutingScheduleModel>> GetRoutingSchedules();
    }

    // Jadwal Routing (Level 2, FASE-3-PRIORITY-TIME.md §3.4): dimuat saat Initialize (start dan
    // RESYNC / Terapkan Perubahan), dievaluasi di memori per transaksi memakai jam server.
    //   Tutup (CLOSED)          : biller dilewati selama jendela berlaku
    //   Jam Operasional (OPEN)  : biller hanya buka di dalam jendelanya; aturan biller + produk
    //                             menggantikan aturan biller untuk semua produk
    //   Prioritas Jadwal        : biller didahulukan; aturan per produk mengalahkan aturan semua
    //                             produk, lalu priority terkecil
    // logika sama dengan SyncNetApi ScheduleEvaluator (dashboard Cek Jadwal).
    public class RoutingScheduleRepository
    {
        private volatile List<RoutingScheduleModel> _rules = [];
        private volatile HashSet<string> _nodes = [];

        private readonly IRoutingScheduleStore _store;
        private readonly Func<DateTime> _clock;

        public RoutingScheduleRepository() : this(new DbMgr(), () => DateTime.Now)
        {
        }

        public RoutingScheduleRepository(IRoutingScheduleStore store, Func<DateTime> clock)
        {
            _store = store;
            _clock = clock;
        }

        public DateTime Now => _clock();

        public async Task Initialize()
        {
            var rules = await _store.GetRoutingSchedules();
            _rules = rules;
            _nodes = rules.Where(r => string.IsNullOrEmpty(r.NodeName) == false)
                .Select(r => r.NodeName).ToHashSet();
        }

        // ada aturan apa pun untuk biller ini (dipakai untuk mencatat siklus Static)
        public bool HasRulesFor(string nodeName)
        {
            return string.IsNullOrEmpty(nodeName) == false && _nodes.Contains(nodeName);
        }

        // null = biller buka; selain itu aturan penyebab tutup
        public RoutingScheduleModel ClosedBy(string routingType, string productId, string nodeName, DateTime t)
        {
            if (HasRulesFor(nodeName) == false) return null;

            var rules = _rules;

            var closed = rules.FirstOrDefault(r => r.RuleType == RoutingScheduleModel.CLOSED
                && r.NodeName == nodeName && InScope(r, routingType, productId) && ScheduleWindow.IsActiveAt(r, t));
            if (closed != null) return closed;

            var open = rules.Where(r => r.RuleType == RoutingScheduleModel.OPEN && r.NodeName == nodeName
                && (r.RoutingType == null || r.RoutingType == routingType)).ToList();
            var specific = open.Where(r => r.InstId == productId).ToList();
            var applicable = specific.Count > 0 ? specific : open.Where(r => r.InstId == null).ToList();

            if (applicable.Count > 0 && applicable.Any(r => ScheduleWindow.IsActiveAt(r, t)) == false)
                return applicable[0];

            return null;
        }

        // priority jadwal yang berlaku untuk biller + produk, atau null
        public short? SchedulePriority(string routingType, string productId, string nodeName, DateTime t)
        {
            if (HasRulesFor(nodeName) == false) return null;

            return _rules.Where(r => r.RuleType == RoutingScheduleModel.PRIORITY && r.Priority.HasValue
                    && r.NodeName == nodeName && InScope(r, routingType, productId)
                    && ScheduleWindow.IsActiveAt(r, t))
                .OrderBy(r => r.InstId == null ? 1 : 0)
                .ThenBy(r => r.Priority)
                .FirstOrDefault()?.Priority;
        }

        private static bool InScope(RoutingScheduleModel r, string routingType, string productId) =>
            (r.RoutingType == null || r.RoutingType == routingType) && (r.InstId == null || r.InstId == productId);

        // terapkan jadwal pada kandidat yang sudah diurutkan menurut Routing Mode:
        // buang yang tutup, lalu biller dengan Prioritas Jadwal dipindah ke depan (stabil).
        // semua tutup -> ScheduleOrder.AllClosed dengan aturan penyebab kandidat pertama.
        public ScheduleOrder<T> Apply<T>(IReadOnlyList<T> ordered, Func<T, string> nodeName, string routingType,
            string productId, bool usePriority = true)
        {
            var t = _clock();
            var open = new List<(T Item, short? Prio, int Index)>();
            RoutingScheduleModel firstClosedBy = null;

            for (int i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i];
                var by = ClosedBy(routingType, productId, nodeName(item), t);
                if (by != null)
                {
                    firstClosedBy ??= by;
                    continue;
                }

                open.Add((item, usePriority == true ? SchedulePriority(routingType, productId, nodeName(item), t) : null, i));
            }

            if (open.Count == 0)
                return new ScheduleOrder<T>([], true, false, firstClosedBy);

            var result = open
                .OrderBy(x => x.Prio.HasValue ? 0 : 1)
                .ThenBy(x => x.Prio ?? 0)
                .ThenBy(x => x.Index)
                .ToList();

            return new ScheduleOrder<T>(result.Select(x => x.Item).ToList(), false, result[0].Prio.HasValue, null);
        }
    }

    // hasil penerapan jadwal. FirstIsScheduled = kandidat pertama berasal dari Prioritas Jadwal
    // (Load Balance tidak dipakai, langsung urutan jadwal).
    public sealed record ScheduleOrder<T>(List<T> Open, bool AllClosed, bool FirstIsScheduled, RoutingScheduleModel ClosedBy);
}
