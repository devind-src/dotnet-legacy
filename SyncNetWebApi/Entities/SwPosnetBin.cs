using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_posnet_bin" — PosBase "Base Config &gt; BIN". id is
    /// `GENERATED ALWAYS AS IDENTITY` in Postgres (verified via information_schema —
    /// column_default is NULL like the non-identity dashboard_role_menu.id, but
    /// identity_generation='ALWAYS'; the two look identical from column_default alone).
    /// Inserting an explicit id fails with 428C9 "cannot insert a non-DEFAULT value into
    /// column \"id\"" unless OVERRIDING SYSTEM VALUE is used — so unlike
    /// dashboard_role_menu.id/sw_term_limit.id, this one MUST be left for EF/DB to
    /// generate. Confirmed by a live insert during Phase 4 smoke testing.</summary>
    [Table("sw_posnet_bin", Schema = "public")]
    public class SwPosnetBin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string participant_id { get; set; } = string.Empty;
        public int group_id { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
