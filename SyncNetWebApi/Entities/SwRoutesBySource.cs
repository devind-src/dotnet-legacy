using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_by_source" — menu Routing &gt; Source (`/route-source`).
    /// `id` was manually computed as max(id)+1 (legacy RouteBySourceCreate, same pattern as
    /// Bank/Brand/sw_job_fee-era tables); converted to a real Postgres IDENTITY column — DB
    /// now assigns it on insert. No FK constraints to sw_nodes/sw_product exist —
    /// legacy only validates via bound dropdowns, replicated here as a manual existence
    /// check in the service layer (same discipline as ConnectionService/node_id).
    /// The real table also has status/created_by/created_dt/updated_by/updated_dt columns,
    /// but the legacy C# model never mapped them and no legacy form ever wrote them — left
    /// unmapped here too, consistent with that.</summary>
    [Table("sw_routes_by_source", Schema = "public")]
    public class SwRoutesBySource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int node_id_in { get; set; } = -1;
        public string? inst_id { get; set; }
        public int node_id_out { get; set; } = -1;
        public string? notes { get; set; }
    }
}
