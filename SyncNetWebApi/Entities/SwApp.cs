using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_app" — Configuration &gt; Interface &gt; Application, new in Phase
    /// 5f. app_name is a manually-assigned string PK (varchar(20)). **14 real production rows
    /// already exist and are actively used by the live switching engine** — never mutate or
    /// delete them outside deliberate user-authorized changes; smoke tests must use a separate
    /// throwaway row. `path` is NOT exposed in the legacy Create/Update form
    /// (SyncNetBlazorServer Interchanges/Application/Detail.razor) — read-only display field
    /// here, never written by this API.</summary>
    [Table("sw_app", Schema = "public")]
    public class SwApp
    {
        [Key]
        public string app_name { get; set; } = string.Empty;
        public string? host { get; set; }
        public string? app_type { get; set; }
        public string? command_port { get; set; }
        public string? path { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public DateTime? last_update { get; set; }
        public string? updated_by { get; set; }
    }
}
