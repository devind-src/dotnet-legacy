using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_products_limit" — Card &gt; Profile &gt; Product &gt; Limit (1:N child
    /// of cms_products, one row per Channel). PK `id`, GENERATED ALWAYS AS IDENTITY — never
    /// assign manually. `id_product` is a REAL FK to cms_products.id with ON DELETE CASCADE —
    /// Postgres auto-deletes these rows when the parent Product is deleted, no app-level
    /// cascade needed. `last_update`/`update_by` are always server-overridden on Create/Update
    /// (matches legacy — never taken from client input). The 31 nr_*/amt_* columns are the
    /// exact same shape as cms_card_override_limits (Card &gt; Profile &gt; Override Limit) —
    /// the two tables are functionally identical limit structures for two different scopes
    /// (per Product+Channel vs per PAN+Channel); the 4-tab Razor markup is shared client-side
    /// (Shared/CardLimitTabs.razor) even though the backend DTOs/entities stay separate.</summary>
    [Table("cms_products_limit", Schema = "public")]
    public class CmsProductLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int? id_product { get; set; }
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
