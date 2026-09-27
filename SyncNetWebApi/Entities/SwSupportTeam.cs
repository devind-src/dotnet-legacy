using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_support_teams" (jamak — legacy [Table] attribute agrees, but legacy
    /// audit label uses singular "sw_support_team", a mismatch — don't repeat it, see
    /// AuditService calls below) — Configuration &gt; Support &gt; Team. team_id was manually computed
    /// as max(id)+1 (legacy quirk); converted to a real Postgres IDENTITY column — DB now
    /// assigns it on insert. status is not in the legacy C# model but exists in the real
    /// table (char(1)) — exposed here as the Active toggle. 0 rows currently.</summary>
    [Table("sw_support_teams", Schema = "public")]
    public class SwSupportTeam
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int team_id { get; set; }
        public string? name { get; set; }
        public string? region { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
