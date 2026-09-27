using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_terminal_client" — PosBase "Base Config &gt; Credential". API
    /// client credentials for POS terminal / partner integration (not database or dashboard
    /// login credentials). secret_key/master_key/session_key/password are generated random
    /// values managed through this CRUD, mirroring the legacy "Generate" buttons.</summary>
    [Table("sw_terminal_client", Schema = "public")]
    public class SwTerminalClient
    {
        [Key]
        public string client_id { get; set; } = string.Empty;
        public string client_name { get; set; } = string.Empty;
        public string? username { get; set; }
        public string? password { get; set; }
        public string? secret_key { get; set; }
        public string? master_key { get; set; }
        public string? session_key { get; set; }
        public string? secret_id { get; set; }
        public string? callback_url { get; set; }
        public string? callback_client_id { get; set; }
        public string? callback_secret_key { get; set; }
        public string? group_name { get; set; }
        public string? subgroup_name { get; set; }
    }
}
