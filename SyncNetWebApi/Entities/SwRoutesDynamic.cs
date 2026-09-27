using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_dynamic" — menu Routing &gt; Dynamic (`/route-dynamic`).
    /// `inst_id` (product code, varchar(11) live-verified) is the PK, immutable after create.
    /// No `node_id` column here — this table only records notes for products routed
    /// dynamically (legacy dropdown restricts suggestions to products whose category has
    /// `is_topup = "1"`, see ProductService.GetTopupProductsAsync).</summary>
    [Table("sw_routes_dynamic", Schema = "public")]
    public class SwRoutesDynamic
    {
        [Key]
        [MaxLength(11)]
        public string inst_id { get; set; } = string.Empty;
        public string? notes { get; set; }
    }
}
