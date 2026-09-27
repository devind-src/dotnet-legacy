using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_by_inst" — menu Routing &gt; Product (`/route-product`), a
    /// per-product-code routing rule (NOT to be confused with the "Product" menu/Product
    /// Master — that is `sw_product`, a different table entirely). `inst_id` (product code,
    /// varchar(11) live-verified) is the PK, immutable after create (legacy renders it
    /// readonly on edit).</summary>
    [Table("sw_routes_by_inst", Schema = "public")]
    public class SwRoutesByInst
    {
        [Key]
        [MaxLength(11)]
        public string inst_id { get; set; } = string.Empty;
        public int node_id { get; set; } = -1;
        public string? notes { get; set; }
        /// <summary>Sharing fee (Rp) dari biller primary untuk produk ini. Dipakai mode
        /// dynamic di Product Fees. Kolom lama `failover_enabled` tidak dipetakan lagi
        /// (digantikan `sw_fees.routing_mode`, di-drop oleh M199).</summary>
        public int? fee_sharing { get; set; }
        /// <summary>Bobot Load Balance primary (0-100).</summary>
        public short? lb_weight { get; set; }
    }
}
