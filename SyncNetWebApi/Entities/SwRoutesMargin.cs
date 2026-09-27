using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_margin" — menu Routing &gt; Margin (`/route-margin`).
    /// `inst_id` (product code, varchar(11) live-verified) is the PK, immutable after create.
    /// No `node_id` column here — this table only records notes for products routed
    /// dynamically (legacy dropdown restricts suggestions to products whose category has
    /// `is_topup = "1"`, see ProductService.GetTopupProductsAsync).</summary>
    [Table("sw_routes_margin", Schema = "public")]
    public class SwRoutesMargin
    {
        [Key]
        [MaxLength(11)]
        public string inst_id { get; set; } = string.Empty;
        public string? notes { get; set; }
        /// <summary>STATIC / PRIORITY / BEST_PRICE / LOAD_BALANCE (diatur di Product &gt; Prices
        /// &gt; Supplier Prices). Menggantikan `failover_enabled` (tidak dipetakan lagi).</summary>
        public string routing_mode { get; set; } = "STATIC";
        /// <summary>Supplier untuk mode STATIC (sw_nodes.node_id). NULL = margin terbesar.</summary>
        public int? static_node_id { get; set; }
    }
}
