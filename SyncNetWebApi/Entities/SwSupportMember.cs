using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_support_members" (jamak) — Configuration &gt; Support &gt; Member.
    /// member_id was manually computed as max(id)+1 (legacy quirk); converted to a real
    /// Postgres IDENTITY column — DB now assigns it on insert. team_id has a REAL Postgres FK
    /// constraint to sw_support_teams.team_id (verified live) — unlike Base's free-text
    /// lookups, this one is DB-enforced, so create/update must validate the team exists and
    /// delete-team must guard against members still referencing it. 0 rows currently.</summary>
    [Table("sw_support_members", Schema = "public")]
    public class SwSupportMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int member_id { get; set; }
        public int team_id { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public string? enabled { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
