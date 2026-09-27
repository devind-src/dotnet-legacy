using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_business_date" — Configuration &gt; Calendar &gt; Business Date, new
    /// in Phase 5d. business_calendar is a manually-assigned string PK (varchar(20)).
    /// current_bsn_date/previous_bsn_date are NOT exposed in the legacy Create/Update form
    /// (SyncNetBlazorServer BusinessDate/Detail.razor) — they are managed by an external
    /// batch/EOD process, not by this dashboard (confirmed live: production row "Default" has
    /// current_bsn_date/previous_bsn_date already populated with no dashboard code path that
    /// writes them). Exposed here read-only; never written by CreateAsync/UpdateAsync.
    /// **1 real production row ("Default") exists — never mutate or delete it outside of
    /// deliberate user-authorized changes; smoke tests must use a separate throwaway row.**</summary>
    [Table("sw_business_date", Schema = "public")]
    public class SwBusinessDate
    {
        [Key]
        public string business_calendar { get; set; } = string.Empty;
        public string? time_cutover { get; set; }
        public string? current_bsn_date { get; set; }
        public string? previous_bsn_date { get; set; }
        public string? enable_closing { get; set; }
        public string? time_start { get; set; }
        public string? time_end { get; set; }
        public string? sun { get; set; }
        public string? mon { get; set; }
        public string? tue { get; set; }
        public string? wed { get; set; }
        public string? thu { get; set; }
        public string? fri { get; set; }
        public string? sat { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
