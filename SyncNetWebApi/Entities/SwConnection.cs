using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_connections" — Configuration &gt; Interface &gt; Nodes &gt; "Add
    /// Connection" (child of sw_nodes, no real FK constraint — verified live). conn_name is
    /// varchar(20), manual PK (no identity concept for a string key). **10 real production rows
    /// already exist and are actively used by the live switching engine** — smoke tests must use
    /// a separate throwaway row. Columns NOT exposed in the legacy Create/Update form
    /// (SyncNetBlazorServer Interchanges/Nodes/Detail.razor) — ws_user, ws_pswd, remote,
    /// ws_header, ws_ipsource, ws_key, ws_proxy_url, ws_proxy_port, status,
    /// last_connected/disconnected (runtime monitoring), created_by/dt, updated_by/dt (not even
    /// mapped by the legacy entity, left to their DB default/NULL) — all read-only or unused
    /// here, defaulted at create from the legacy SwConnection() constructor, never written by
    /// Update.</summary>
    [Table("sw_connections", Schema = "public")]
    public class SwConnection
    {
        [Key]
        [MaxLength(20)]
        public string conn_name { get; set; } = string.Empty;
        public int? node_id { get; set; }
        public string? protocol { get; set; }
        public string? tcp_header_format { get; set; }
        public string? tcp_footer { get; set; }
        public string? conn_type { get; set; }
        public string? ip_address { get; set; }
        public string? port { get; set; }
        public int? max_conn { get; set; }
        public int? retry_delay { get; set; }
        public string? always_connected { get; set; }
        public string? queue_inbox { get; set; }
        public string? queue_outbox { get; set; }
        public string? ws_url { get; set; }
        public string? ws_user { get; set; }
        public string? ws_pswd { get; set; }
        public short? remote { get; set; }
        public DateTime? last_connected { get; set; }
        public DateTime? last_disconnected { get; set; }
        public string? ws_header { get; set; }
        public string? ws_method { get; set; }
        public string? ws_content { get; set; }
        public string? ws_ipsource { get; set; }
        public string? ws_key { get; set; }
        public short tcp_hi_lo { get; set; }
        public string? ws_proxy_url { get; set; }
        public string? ws_proxy_port { get; set; }
        public string? one_socket_only { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
