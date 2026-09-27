using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_jobs" — Configuration &gt; Jobs &gt; Job Schedule, new in Phase 5e.
    /// job_id was manually computed as max(id)+1 (legacy quirk); converted to a real Postgres
    /// IDENTITY column — DB now assigns it on insert, sequence seeded past the existing rows.
    /// **2 real production rows exist
    /// ("Clean Transaction", "Update Fees") already run by an external Middleware/Back-Office
    /// process** — never mutate/delete them outside deliberate user-authorized changes; smoke
    /// tests must use a separate throwaway row. `status`/`read_only` are NOT exposed in the
    /// legacy Create/Update form (SyncNetBlazorServer JobSchedule/Detail.razor) — `status` holds
    /// the last-run result text ("Done"/null), `read_only` is an internal built-in-job flag —
    /// both read-only display fields here, never written by this API.</summary>
    [Table("sw_jobs", Schema = "public")]
    public class SwJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int job_id { get; set; }
        public string? job_name { get; set; }
        public string? freq_flag { get; set; }
        public string? freq_once { get; set; }
        public int? freq_number { get; set; }
        public string? freq_start { get; set; }
        public string? freq_end { get; set; }
        public string? enabled { get; set; }
        public DateTime? last_running { get; set; }
        public string? status { get; set; }
        public string? read_only { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
        public string? app_path { get; set; }
        public string? run_at { get; set; }
    }
}
