using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_margin_merchant" — menu "Product &gt; Pricing &amp; Fees &gt; Merchant Pricing"
    /// (`/product/pricing-fees/merchant-pricing`). `id` (int) was manually computed as max(id)+1 (legacy
    /// quirk); converted to a real Postgres IDENTITY column — DB now assigns it on insert.
    /// `merchant_id`/`biller_code` are immutable after create.
    /// `product_name` is denormalized: resolved from Product Master and stored on save, not
    /// kept in sync afterwards (same pattern as sw_margin_supplier). `status` exists on the
    /// real table and legacy's constructor defaults it to "1", but **no legacy form ever
    /// exposes it for editing** — always written as "1" on create here, never touched by
    /// update. The real table also has a `margin` column, but legacy's Save() never computes
    /// or writes it for this module (unlike Supplier Prices) — confirmed live: the one
    /// production row has `margin = NULL`. Left unmapped here to match that dead-column
    /// behavior. Widths verified live: merchant_id/biller_code varchar(15), product_name
    /// varchar(30), status varchar(2).</summary>
    [Table("sw_margin_merchant", Schema = "public")]
    public class SwMarginMerchant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string? merchant_id { get; set; }
        public string? product_name { get; set; }
        public string? biller_code { get; set; }
        public int? denom { get; set; }
        public int? harga_jual { get; set; }
        public string? status { get; set; }
    }
}
