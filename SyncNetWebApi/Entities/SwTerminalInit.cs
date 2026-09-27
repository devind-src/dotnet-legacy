using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_term_init" — PosBase &gt; Terminal &gt; "Device" tab (Enable Init / ID
    /// Init fields).</summary>
    [Table("sw_term_init", Schema = "public")]
    public class SwTerminalInit
    {
        [Key]
        public string term_id { get; set; } = string.Empty;
        public string? init_id { get; set; }
        public DateTime? date_init { get; set; }
        public string? enable_init { get; set; }
    }
}
