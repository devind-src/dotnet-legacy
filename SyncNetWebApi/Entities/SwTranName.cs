using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_tran_names" (jamak) — Configuration &gt; Base &gt; Tran Names.
    /// trans_code is char(2) NOT NULL (PK). trans_name is varchar(50) NOT NULL — column widths
    /// verified live via information_schema.</summary>
    [Table("sw_tran_names", Schema = "public")]
    public class SwTranName
    {
        [Key]
        public string trans_code { get; set; } = string.Empty;
        public string trans_name { get; set; } = string.Empty;
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
