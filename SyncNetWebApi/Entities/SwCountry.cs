using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_countries" (jamak — bukan "sw_country", lihat catatan audit di
    /// HANDOFF_PHASE5_CONFIGURATION.md §4) — Configuration &gt; Base &gt; Country. "name" adalah
    /// PK varchar(50). code_alpha_2 is char(2) NOT NULL, code_alpha_3 is char(3) NOT NULL,
    /// code_numeric is integer NOT NULL — column widths verified live via information_schema.</summary>
    [Table("sw_countries", Schema = "public")]
    public class SwCountry
    {
        [Key]
        public string name { get; set; } = string.Empty;
        public string code_alpha_2 { get; set; } = string.Empty;
        public string code_alpha_3 { get; set; } = string.Empty;
        public int code_numeric { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
