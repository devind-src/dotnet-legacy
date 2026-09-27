using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_nodes" — Configuration &gt; Interface &gt; Nodes, new in Phase 5f.
    /// node_id was manually computed as max(id)+1 (legacy quirk), same pattern as Bank/Brand;
    /// converted to a real Postgres IDENTITY column — DB now assigns it on insert, sequence
    /// seeded past the existing rows. **10 real production rows already exist and are actively
    /// used by the live switching engine** — never mutate or delete them outside deliberate
    /// user-authorized changes; smoke tests must use a separate throwaway row.
    /// Many columns are NOT exposed in the legacy Create/Update form
    /// (SyncNetBlazorServer Interchanges/Nodes/Master.razor) — pro_mgr, send_cutover_msg,
    /// security_profile, fds_profile, remote, conn_in, conn_out, signon, saf, last_cutover,
    /// provider_service, auth_service, issuer, limit_class, auth_resp, allocate_trace,
    /// last_connected/disconnected/echo (runtime monitoring), status (no Active toggle exists
    /// in the legacy form at all) — all read-only here, defaulted at create from the legacy
    /// constructor's values, never written by Update. port_in/port_out are also form-readonly
    /// (auto-assigned the next sequential port, never user-editable).</summary>
    [Table("sw_nodes", Schema = "public")]
    public class SwNodes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int node_id { get; set; }
        public string node_name { get; set; } = string.Empty;
        public string? app_name { get; set; }
        public string? pro_mgr { get; set; }
        public string? port_in { get; set; }
        public string? port_out { get; set; }
        public string? parameter { get; set; }
        public string? inst_id { get; set; }
        public string? auto_signon { get; set; }
        public string? auto_reversal { get; set; }
        public string? auto_reply_reversal { get; set; }
        public string? save_repeat_reversal { get; set; }
        public string? send_cutover_msg { get; set; }
        public short? keychange_timer { get; set; }
        public short? echo_timer { get; set; }
        public short? request_timeout { get; set; }
        public short? advice_timeout { get; set; }
        public string? pin_translate { get; set; }
        public string? business_calendar { get; set; }
        public string? security_profile { get; set; }
        public string? fds_profile { get; set; }
        public short? remote { get; set; }
        public short? conn_in { get; set; }
        public short? conn_out { get; set; }
        public short? signon { get; set; }
        public int? saf { get; set; }
        public int? saf_limit { get; set; }
        public string? last_cutover { get; set; }
        public string? provider_service { get; set; }
        public string? auth_service { get; set; }
        public string? issuer { get; set; }
        public string? limit_class { get; set; }
        public string? auth_resp { get; set; }
        public int? team_id { get; set; }
        public int? sensitive_data { get; set; }
        public string? allocate_trace { get; set; }
        public DateTime? last_connected { get; set; }
        public DateTime? last_disconnected { get; set; }
        public DateTime? last_echo { get; set; }
        public string? category { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
