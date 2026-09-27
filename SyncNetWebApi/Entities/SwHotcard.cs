using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_hotcard" — Card &gt; Base &gt; Hotcard. card_nr is varchar(19) NOT
    /// NULL (PK, manual — legacy HTML form used maxlength=16, narrower than the real column),
    /// but the legacy business rule still requires the value to be exactly 16 characters —
    /// replicated as a request-level custom validation, not a MaxLength/width constraint.
    /// No FK to any other Card table. Real table also has status/created_by/created_dt/
    /// updated_by/updated_dt columns, never mapped or written by the legacy model/form — dead
    /// columns, not mapped here.</summary>
    [Table("sw_hotcard", Schema = "public")]
    public class SwHotcard
    {
        [Key]
        [MaxLength(19)]
        public string card_nr { get; set; } = string.Empty;
        [MaxLength(2)]
        public string? resp_code { get; set; }
        [MaxLength(6)]
        public string? auth_id_resp { get; set; }
        [MaxLength(50)]
        public string? notes { get; set; }
    }
}
