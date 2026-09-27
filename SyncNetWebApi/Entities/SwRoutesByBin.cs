using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_by_bin" — menu Routing &gt; BIN (`/route-bin`). `group_id` is
    /// the PK and is NOT server-generated — it is the Card Group (`sw_group.group_id`) the
    /// admin picks from a dropdown at create, immutable after (legacy disables the select on
    /// edit). No FK constraint to sw_group/sw_nodes exists at the DB level.</summary>
    [Table("sw_routes_by_bin", Schema = "public")]
    public class SwRoutesByBin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int group_id { get; set; } = -1;
        public int node_id { get; set; } = -1;
    }
}
