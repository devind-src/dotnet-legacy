using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "va_account" — Virtual Account &gt; Account. Upgraded from a read-only
    /// lookup (still backs the "Virtual Account" dropdown on Merchant/Terminal forms via
    /// LookupsController — untouched) to full CRUD in the Virtual Account phase.
    /// acc_nr is the PK (varchar(30), manual — legacy generates a random 10-digit number via
    /// a "Generate" button, no identity involved). group_name is immutable after create
    /// (disabled in the legacy Edit form) and has a real FK to va_group.group_name (ON DELETE
    /// NO ACTION). balance is never written by the account form itself — it only changes via
    /// the Approval workflow. Legacy's raw UPDATE only ever touches va_notes/low_balance/
    /// min_balance/status, so those are the only fields Update writes here too.
    /// Real table also has product_id/created_by/created_dt/last_update/updated_by columns
    /// that the legacy model/form never mapped or wrote — dead columns, not mapped here
    /// either (same pattern as other modules).</summary>
    [Table("va_account", Schema = "public")]
    public class VaAccount
    {
        [Key]
        [MaxLength(30)]
        public string acc_nr { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? group_name { get; set; }

        [MaxLength(30)]
        public string? va_name { get; set; }

        [MaxLength(50)]
        public string? va_notes { get; set; }

        [MaxLength(1)]
        public string? status { get; set; }

        public decimal? low_balance { get; set; }
        public decimal? min_balance { get; set; }
        public decimal? balance { get; set; }
    }
}
