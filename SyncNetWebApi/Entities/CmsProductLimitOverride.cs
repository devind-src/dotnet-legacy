using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_card_override_limits" — Card &gt; Profile &gt; Override Limit
    /// (standalone, keyed by Issuer+PAN+Channel per row, no real FK to any other Card table).
    /// PK `id`, GENERATED ALWAYS AS IDENTITY — never assign manually. Same 31 nr_*/amt_*
    /// column shape as cms_products_limit (see that entity's note) — the 4-tab Razor markup is
    /// shared client-side (Shared/CardLimitTabs.razor). `last_update`/`update_by` are always
    /// server-overridden on Create/Update, matching legacy.</summary>
    [Table("cms_card_override_limits", Schema = "public")]
    public class CmsProductLimitOverride
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(30)]
        public string? issuer { get; set; }
        [MaxLength(16)]
        public string pan { get; set; } = string.Empty;
        [MaxLength(4)]
        public string? channel { get; set; }

        public int? amt_purchase_per_tran { get; set; }
        public int? amt_cash_per_tran { get; set; }
        public int? amt_payment_per_tran { get; set; }
        public int? amt_transfer_per_tran { get; set; }

        public int? nr_inquiry_daily { get; set; }
        public int? nr_purchase_daily { get; set; }
        public int? nr_cash_daily { get; set; }
        public int? nr_payment_daily { get; set; }
        public int? nr_transfer_daily { get; set; }
        public int? amt_purchase_daily { get; set; }
        public int? amt_cash_daily { get; set; }
        public int? amt_payment_daily { get; set; }
        public int? amt_transfer_daily { get; set; }

        public int? nr_inquiry_weekly { get; set; }
        public int? nr_purchase_weekly { get; set; }
        public int? nr_cash_weekly { get; set; }
        public int? nr_payment_weekly { get; set; }
        public int? nr_transfer_weekly { get; set; }
        public int? amt_purchase_weekly { get; set; }
        public int? amt_cash_weekly { get; set; }
        public int? amt_payment_weekly { get; set; }
        public int? amt_transfer_weekly { get; set; }

        public int? nr_inquiry_monthly { get; set; }
        public int? nr_purchase_monthly { get; set; }
        public int? nr_cash_monthly { get; set; }
        public int? nr_payment_monthly { get; set; }
        public int? nr_transfer_monthly { get; set; }
        public int? amt_purchase_monthly { get; set; }
        public int? amt_cash_monthly { get; set; }
        public int? amt_payment_monthly { get; set; }
        public int? amt_transfer_monthly { get; set; }

        public DateTime? last_update { get; set; }
        [MaxLength(30)]
        public string? update_by { get; set; }
    }
}
