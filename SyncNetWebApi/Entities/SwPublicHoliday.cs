using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_public_holiday" — Configuration &gt; Calendar &gt; Public Holiday,
    /// new in Phase 5d. holiday_date is a manually-assigned string PK (varchar(10), format
    /// "yyyy-MM-dd" per legacy Save()). 0 rows currently.</summary>
    [Table("sw_public_holiday", Schema = "public")]
    public class SwPublicHoliday
    {
        [Key]
        public string holiday_date { get; set; } = string.Empty;
        public string? holiday_name { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
