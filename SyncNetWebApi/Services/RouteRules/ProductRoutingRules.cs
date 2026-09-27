using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Entities;

namespace SyncNetApi.Services.RouteRules
{
    /// <summary>Satu biller produk bill payment &amp; purchase: primary (sw_routes_by_inst,
    /// priority 1) atau alternate (sw_routes_by_inst_alt).</summary>
    public record ProductBillerInfo(int NodeId, string NodeName, int Priority, bool IsPrimary,
        int? FeeSharing, int? LbWeight, bool Active);

    /// <summary>Aturan Routing Mode Product Fees (keputusan fase 1 K1-K22): baris fee per produk
    /// (default + per CA) harus konsisten dengan daftar biller produk itu.
    /// <list type="bullet">
    /// <item>Mode dynamic (Priority/Best Price/Load Balance) wajib Fee Type Fixed, butuh minimal 2
    /// biller aktif, dan tidak boleh untuk produk yang punya rule di Routing &gt; Source.</item>
    /// <item>Fee Switch tidak boleh negatif: untuk setiap biller aktif, Loket + Mitra + Issuer
    /// &lt;= Sharing &lt;= Total.</item>
    /// <item>Load Balance: total bobot biller aktif tepat 100.</item>
    /// <item>Static: biller pilihan (bila diisi) harus biller produk ini.</item>
    /// </list></summary>
    public static class ProductRoutingRules
    {
        private const int MinBillersForDynamic = 2;

        public static bool IsDefaultRow(SwFees fee)
            => string.IsNullOrEmpty(fee.merchant_id) && string.IsNullOrEmpty(fee.submerchant_id);

        /// <summary>Mode yang berlaku untuk sebuah baris: baris default null = STATIC, baris CA
        /// null = Ikuti Default.</summary>
        public static string EffectiveMode(SwFees fee, SwFees? defaultRow)
        {
            if (!string.IsNullOrEmpty(fee.routing_mode)) return fee.routing_mode;
            if (IsDefaultRow(fee)) return RoutingModes.Static;
            return string.IsNullOrEmpty(defaultRow?.routing_mode) ? RoutingModes.Static : defaultRow.routing_mode;
        }

        public static string RowLabel(SwFees fee)
        {
            if (IsDefaultRow(fee)) return "default";
            return string.IsNullOrEmpty(fee.submerchant_id) ? $"CA {fee.merchant_id}" : $"CA {fee.merchant_id}/{fee.submerchant_id}";
        }

        public static async Task<List<ProductBillerInfo>> LoadBillersAsync(SyncNetDbContext context, string productId)
        {
            var primary = await context.RoutesByInst.AsNoTracking().FirstOrDefaultAsync(r => r.inst_id == productId);
            var alternates = await context.RoutesByInstAlt.AsNoTracking().Where(a => a.inst_id == productId).ToListAsync();

            return await BuildBillersAsync(context, primary, alternates);
        }

        /// <summary>Susun daftar biller dari entity (termasuk perubahan yang belum disimpan).</summary>
        public static async Task<List<ProductBillerInfo>> BuildBillersAsync(SyncNetDbContext context,
            SwRoutesByInst? primary, IEnumerable<SwRoutesByInstAlt> alternates)
        {
            var alts = alternates.ToList();
            var nodeIds = alts.Select(a => a.node_id).ToList();
            if (primary != null) nodeIds.Add(primary.node_id);

            var names = await context.Nodes.AsNoTracking()
                .Where(n => nodeIds.Contains(n.node_id))
                .ToDictionaryAsync(n => n.node_id, n => n.node_name);

            var list = new List<ProductBillerInfo>();
            if (primary != null)
            {
                list.Add(new ProductBillerInfo(primary.node_id, names.GetValueOrDefault(primary.node_id, $"#{primary.node_id}"),
                    1, true, primary.fee_sharing, primary.lb_weight, true));
            }

            list.AddRange(alts
                .OrderBy(a => a.priority)
                .Select(a => new ProductBillerInfo(a.node_id, names.GetValueOrDefault(a.node_id, $"#{a.node_id}"),
                    a.priority, false, a.fee_sharing, a.lb_weight, a.status == "1")));

            return list;
        }

        /// <summary>Periksa semua baris fee produk terhadap daftar biller. checkLbTotal = false
        /// saat mengubah satu biller (bobot diubah bertahap; total diperiksa saat baris fee
        /// disimpan atau bobot diubah sekaligus). editedNodeId diisi saat mengubah satu biller:
        /// sharing fee yang wajib terisi hanya milik biller itu, supaya sharing fee biller bisa
        /// diisi satu per satu.</summary>
        public static async Task EnsureConsistentAsync(SyncNetDbContext context, string productId,
            IReadOnlyCollection<SwFees> rows, IReadOnlyCollection<ProductBillerInfo> billers, bool checkLbTotal,
            int? editedNodeId = null)
        {
            var defaultRow = rows.FirstOrDefault(IsDefaultRow);
            var active = billers.Where(b => b.Active).ToList();
            bool anyDynamic = false;

            foreach (var row in rows)
            {
                string mode = EffectiveMode(row, defaultRow);
                string label = RowLabel(row);

                if (!RoutingModes.IsDynamic(mode))
                {
                    if (row.static_node_id.HasValue && billers.All(b => b.NodeId != row.static_node_id.Value))
                        throw new ValidationException(
                            $"Baris {label}: biller Static bukan biller produk '{productId}'. Pilih dari daftar biller produk ini.");
                    continue;
                }

                anyDynamic = true;
                string modeLabel = RoutingModes.Label(mode);

                if ((row.fee_type ?? "0") != "0")
                    throw new ValidationException(
                        $"Baris {label}: Fee Type Percent hanya untuk routing Static, tidak bisa dipakai dengan mode {modeLabel}.");

                if (active.Count < MinBillersForDynamic)
                    throw new ValidationException(
                        $"Mode {modeLabel} butuh minimal {MinBillersForDynamic} biller aktif untuk produk '{productId}' (sekarang {active.Count}).");

                int total = row.fixed_fee ?? 0;
                int deductions = (row.fixed_fee_acq ?? 0) + (row.fixed_fee_mer ?? 0) + (row.fixed_fee_iss ?? 0);

                if (total <= 0)
                    throw new ValidationException(
                        $"Baris {label}: Total Fee wajib diisi (lebih dari 0) untuk mode {modeLabel}.");

                if (deductions > total)
                    throw new ValidationException(
                        $"Baris {label}: Fee Loket + Fee Mitra ({deductions:N0}) melebihi Total Fee ({total:N0}).");

                foreach (var biller in active)
                {
                    if (!biller.FeeSharing.HasValue)
                    {
                        if (editedNodeId.HasValue && biller.NodeId != editedNodeId.Value) continue;

                        throw new ValidationException(
                            $"Sharing fee biller {biller.NodeName} belum diisi. Mode {modeLabel} menghitung Fee Biller dan Fee Switch dari sharing fee.");
                    }

                    int sharing = biller.FeeSharing.Value;
                    if (sharing > total)
                        throw new ValidationException(
                            $"Sharing fee biller {biller.NodeName} ({sharing:N0}) melebihi Total Fee baris {label} ({total:N0}), Fee Biller menjadi negatif.");

                    if (sharing < deductions)
                        throw new ValidationException(
                            $"Fee Switch baris {label} untuk biller {biller.NodeName} menjadi negatif: sharing {sharing:N0} lebih kecil dari Fee Loket + Fee Mitra ({deductions:N0}).");
                }

                if (checkLbTotal && mode == RoutingModes.LoadBalance)
                {
                    int weight = active.Sum(b => b.LbWeight ?? 0);
                    if (weight != 100)
                        throw new ValidationException(
                            $"Mode Load Balance butuh total LB Weight biller aktif tepat 100% (sekarang {weight}%). Gunakan tombol Atur LB Weight untuk mengubah bobot semua biller sekaligus.");
                }
            }

            if (anyDynamic && await RouteCategoryRules.HasSourceRuleAsync(context, productId))
                throw new ValidationException(
                    $"Produk '{productId}' punya rule di Routing > Source. Mode dynamic tidak bisa dipakai karena push route mengalahkan route by source.");
        }
    }
}
