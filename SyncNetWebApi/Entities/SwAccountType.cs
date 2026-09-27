using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_account_types" — Configuration &gt; Base &gt; Account Type.
    /// acct_type is char(2) NOT NULL (PK, manual, not identity). name is varchar(20) NOT NULL.
    /// Column widths verified live via information_schema.</summary>
    [Table("sw_account_types", Schema = "public")]
    public class SwAccountType
    {
        [Key]
        public string acct_type { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
