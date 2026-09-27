using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_group" — Card &gt; Base &gt; Group. Still backs the "Group"/"Card Group"
    /// dropdown on PosBase &gt; Posnet BIN and Routing &gt; BIN via LookupsController. group_id was
    /// manually computed as max(group_id)+1 (legacy quirk); converted to a real Postgres
    /// IDENTITY column — DB now assigns it on insert. Note: sw_routes_by_bin.group_id stores a
    /// copy of this value (admin-picked FK, no DB-level constraint) and is intentionally NOT an
    /// identity column itself. Real table also has status/created_by/created_dt/updated_by/
    /// updated_dt columns, but the legacy model/form never mapped or wrote them — dead columns,
    /// not mapped here either (same pattern as the Routing tables).</summary>
    [Table("sw_group", Schema = "public")]
    public class SwGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int group_id { get; set; }
        public string? inst_id { get; set; }
        public string? group_name { get; set; }
    }
}
