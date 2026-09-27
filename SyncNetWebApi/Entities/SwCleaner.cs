using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_cleaner" — Configuration &gt; Jobs &gt; Job Cleaner, new in Phase 5e.
    /// entity is a manually-assigned string PK (varchar(30)). **1 real production row exists
    /// ("Transactions", period 360 days)** — never mutate/delete it outside deliberate
    /// user-authorized changes; smoke tests must use a separate throwaway row.</summary>
    [Table("sw_cleaner", Schema = "public")]
    public class SwCleaner
    {
        [Key]
        public string entity { get; set; } = string.Empty;
        public int? period { get; set; }
        public string? description { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
