using System;
using System.Collections.Generic;
using System.Linq;
using SyncNetApi.Dtos.RouteSchedules;

namespace SyncNetApi.Services.RouteSchedules
{
    /// <summary>Biller kandidat satu produk. Value = sharing fee (bill payment) atau margin (topup)
    /// untuk Best Price; BuyPrice = harga beli topup (tie-break Best Price).</summary>
    public sealed record ScheduleCandidate(int NodeId, string NodeName, int BasePriority, int? Value, int? BuyPrice, int LbWeight);

    public sealed record ScheduleProductInput(
        string ProductId, string? ProductName, string RoutingType, string RoutingMode, int? StaticNodeId,
        int? Denom, bool EmergencyOff, IReadOnlyList<ScheduleCandidate> Candidates);

    /// <summary>Evaluasi jadwal untuk satu produk pada satu waktu (FASE-3 §3.4 dan §4), tanpa status
    /// health: biller Tutup / di luar Jam Operasional dikeluarkan, sisanya diurutkan menurut Routing
    /// Mode lalu biller dengan Prioritas Jadwal aktif dipindah ke depan. Tidak ada biller buka =
    /// ditolak (X15). Static: biller static tutup = ditolak; Prioritas Jadwal diabaikan. Tombol
    /// darurat OFF: jadwal diabaikan (routing statis).</summary>
    public sealed class ScheduleEvaluator
    {
        private readonly IReadOnlyList<ScheduleRule> _rules;

        public ScheduleEvaluator(IEnumerable<ScheduleRule> rules)
        {
            _rules = rules.Where(r => r.IsActive).ToList();
        }

        private static bool InScope(ScheduleRule r, string routingType, string productId, int nodeId) =>
            (r.RoutingType == null || r.RoutingType == routingType)
            && (r.InstId == null || r.InstId == productId)
            && (r.NodeId == null || r.NodeId == nodeId);

        /// <summary>Null = buka; selain itu teks penyebab tutup (nama aturan).</summary>
        public string? ClosedBy(string routingType, string productId, int nodeId, DateTime t)
        {
            var closed = _rules.FirstOrDefault(r => r.RuleType == RouteScheduleCodes.Closed
                && InScope(r, routingType, productId, nodeId) && ScheduleWindow.IsActiveAt(r, t));
            if (closed != null) return closed.Name;

            // Jam Operasional: aturan biller + produk menggantikan aturan biller untuk semua produk
            var open = _rules.Where(r => r.RuleType == RouteScheduleCodes.Open && r.NodeId == nodeId
                && (r.RoutingType == null || r.RoutingType == routingType)).ToList();
            var specific = open.Where(r => r.InstId == productId).ToList();
            var applicable = specific.Count > 0 ? specific : open.Where(r => r.InstId == null).ToList();

            if (applicable.Count > 0 && !applicable.Any(r => ScheduleWindow.IsActiveAt(r, t)))
                return $"Di luar jam operasional ({string.Join(", ", applicable.Select(r => r.Name).Distinct())})";

            return null;
        }

        /// <summary>Priority jadwal yang aktif untuk biller + produk, atau null. Aturan untuk produk ini
        /// mengalahkan aturan biller untuk semua produk (R9); di antara yang setara dipakai priority terkecil.</summary>
        public (short Priority, string By)? SchedulePriority(string routingType, string productId, int nodeId, DateTime t)
        {
            var hit = _rules.Where(r => r.RuleType == RouteScheduleCodes.Priority && r.Priority.HasValue
                    && r.NodeId == nodeId && (r.InstId == null || r.InstId == productId)
                    && (r.RoutingType == null || r.RoutingType == routingType)
                    && ScheduleWindow.IsActiveAt(r, t))
                .OrderBy(r => r.InstId == null ? 1 : 0)
                .ThenBy(r => r.Priority)
                .FirstOrDefault();
            return hit == null ? null : (hit.Priority!.Value, hit.Name);
        }

        public ScheduleCheckProductDto Evaluate(ScheduleProductInput p, DateTime t, IReadOnlyDictionary<string, string>? health = null)
        {
            string mode = string.IsNullOrEmpty(p.RoutingMode) ? "STATIC" : p.RoutingMode;
            var ordered = Order(p.Candidates, mode, p.RoutingType);

            var rows = ordered.Select(c =>
            {
                string? closedBy = p.EmergencyOff ? null : ClosedBy(p.RoutingType, p.ProductId, c.NodeId, t);
                var prio = p.EmergencyOff || mode == "STATIC" ? null : SchedulePriority(p.RoutingType, p.ProductId, c.NodeId, t);
                return (c, closedBy, prio);
            }).ToList();

            string? H(string node) => health != null && health.TryGetValue(node, out var s) ? s : null;

            if (p.Candidates.Count == 0)
                return new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Reject, null,
                    p.RoutingType == RouteScheduleCodes.Margin
                        ? $"Belum ada Supplier Price aktif{(p.Denom.HasValue ? $" untuk denom {p.Denom:N0}" : "")}: atur di Product > Pricing & Fees > Supplier Prices. Transaksi produk ini tidak bisa dirutekan."
                        : "Belum ada biller: atur di Product > Pricing & Fees > Product Fees (biller produk).", []);

            if (p.EmergencyOff)
            {
                var first = ordered[0];
                return new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Core, first.NodeName,
                    "Tombol darurat OFF: jadwal dan health check diabaikan.",
                    rows.Select((x, i) => ToBiller(x.c, true, null, null, i + 1, H(x.c.NodeName))).ToList());
            }

            if (mode == "STATIC")
            {
                var target = (p.StaticNodeId.HasValue ? ordered.FirstOrDefault(c => c.NodeId == p.StaticNodeId) : null) ?? ordered[0];
                var row = rows.First(x => x.c.NodeId == target.NodeId);
                bool open = row.closedBy == null;
                var billers = rows.Select(x => ToBiller(x.c, x.closedBy == null, x.closedBy, null,
                    x.c.NodeId == target.NodeId && open ? 1 : null, H(x.c.NodeName))).ToList();

                return open
                    ? new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Route, target.NodeName,
                        "Static: selalu ke satu biller, tanpa failover.", billers)
                    : new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Reject, null,
                        $"Static: biller {target.NodeName} tutup ({row.closedBy}) → ditolak X15.", billers);
            }

            // biller dengan Prioritas Jadwal aktif di depan (menurut priority jadwal), sisanya urutan mode
            var openRows = rows.Where(x => x.closedBy == null)
                .Select((x, i) => (x, i))
                .OrderBy(y => y.x.prio.HasValue ? 0 : 1)
                .ThenBy(y => y.x.prio?.Priority ?? 0)
                .ThenBy(y => y.i)
                .Select(y => y.x)
                .ToList();

            var rank = openRows.Select((x, i) => (x.c.NodeId, Rank: i + 1)).ToDictionary(k => k.NodeId, v => v.Rank);
            var result = rows.Select(x => ToBiller(x.c, x.closedBy == null, x.closedBy, x.prio,
                rank.TryGetValue(x.c.NodeId, out var rk) ? rk : null, H(x.c.NodeName))).ToList();

            if (openRows.Count == 0)
                return new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Reject, null,
                    "Semua biller tutup → ditolak X15.", result);

            string? note = null;
            if (mode == "LOAD_BALANCE" && !openRows[0].prio.HasValue)
            {
                int total = openRows.Sum(x => Math.Max(x.c.LbWeight, 0));
                note = total > 0
                    ? "Load Balance: " + string.Join(", ", openRows.Select(x => $"{x.c.NodeName} {Math.Round(100m * Math.Max(x.c.LbWeight, 0) / total)}%"))
                    : "Load Balance: bobot kosong, ke urutan pertama.";
            }
            else if (openRows[0].prio.HasValue)
            {
                note = $"Prioritas Jadwal '{openRows[0].prio!.Value.By}' aktif.";
            }

            return new(p.ProductId, p.ProductName, p.RoutingType, mode, p.Denom, ScheduleCheckResults.Route,
                openRows[0].c.NodeName, note, result);
        }

        private static ScheduleCheckBillerDto ToBiller(ScheduleCandidate c, bool open, string? closedBy,
            (short Priority, string By)? prio, int? rank, string? health) =>
            new(c.NodeId, c.NodeName, c.BasePriority, open, closedBy, prio?.Priority, prio?.By, rank, health);

        // urutan Routing Mode, sama dengan ProductRoutingStrategy / MarginRoutingStrategy di SDK
        private static List<ScheduleCandidate> Order(IReadOnlyList<ScheduleCandidate> candidates, string mode, string routingType)
        {
            if (mode == "BEST_PRICE" || (mode == "STATIC" && routingType == RouteScheduleCodes.Margin))
            {
                return candidates
                    .OrderByDescending(c => c.Value.HasValue)
                    .ThenByDescending(c => c.Value ?? 0)
                    .ThenBy(c => c.BuyPrice ?? int.MaxValue)
                    .ThenBy(c => c.BasePriority)
                    .ToList();
            }

            return candidates.OrderBy(c => c.BasePriority).ToList();
        }
    }
}
