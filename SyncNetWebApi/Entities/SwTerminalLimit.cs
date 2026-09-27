using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_term_limit" — PosBase "Base Config &gt; Limit". id (bigint) was
    /// manually computed as max(id)+1 (legacy quirk); converted to a real Postgres IDENTITY
    /// column — DB now assigns it on insert, same as SwPosnetBin.id.</summary>
    [Table("sw_term_limit", Schema = "public")]
    public class SwTerminalLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? limit_name { get; set; }
        public string? group_name { get; set; }
        public long min_withdrawal { get; set; }
        public long max_withdrawal { get; set; }
        public long min_transfer { get; set; }
        public long max_transfer { get; set; }
        public long min_purchase { get; set; }
        public long max_purchase { get; set; }
        public long min_payment { get; set; }
        public long max_payment { get; set; }
    }
}
