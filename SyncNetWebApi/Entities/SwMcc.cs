using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_mcc" — Configuration &gt; Base &gt; MCC. Upgraded from Phase 4's
    /// read-only lookup to full CRUD in Phase 5b. mcc_code is a manually-assigned string PK
    /// (varchar(5), n/a for identity). floor_limit/currency were missing from the Phase 4
    /// read-only mapping — added here (verified live via information_schema: floor_limit
    /// varchar(5), currency varchar(4)). Still backs the read-only MCC autocomplete on Merchant
    /// forms via LookupsController.</summary>
    [Table("sw_mcc", Schema = "public")]
    public class SwMcc
    {
        [Key]
        public string mcc_code { get; set; } = string.Empty;
        public string? mcc_desc { get; set; }
        public string? floor_limit { get; set; }
        public string? currency { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
