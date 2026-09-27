using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_margin_supplier" — menu "Product &gt; Pricing &amp; Fees &gt; Prepaid Pricing"
    /// (`/product/pricing-fees/prepaid-pricing`). `id` (int) was manually computed as max(id)+1 (legacy
    /// quirk); converted to a real Postgres IDENTITY column — DB now assigns it on insert.
    /// `supplier_id` is free text copied from a Node-Biller dropdown
    /// (not a real FK, no existence check in legacy either — only "mandatory"), immutable after
    /// create together with `biller_code` (product code). `product_name` is denormalized:
    /// resolved from Product Master and stored on save (legacy `ProductMasterGetProductName`),
    /// not kept in sync afterwards. `margin` is computed server-side as `harga_jual -
    /// harga_beli`, never user-input (the form field is commented out in legacy). The real
    /// table also has `update_by`/`update_datetime` columns the legacy model never mapped —
    /// left unmapped here too. Widths verified live: supplier_id/biller_code varchar(15),
    /// product_name varchar(30), status varchar(2).</summary>
    [Table("sw_margin_supplier", Schema = "public")]
    public class SwMarginSupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string? supplier_id { get; set; }
        public string? product_name { get; set; }
        public string? biller_code { get; set; }
        public int? denom { get; set; }
        public int? harga_beli { get; set; }
        public int? harga_jual { get; set; }
        public int? margin { get; set; }
        public string? status { get; set; }
        /// <summary>Priority supplier per produk + denom (mode PRIORITY, tie-break BEST_PRICE).</summary>
        public short? priority { get; set; }
        /// <summary>Bobot Load Balance per produk + denom (0-100).</summary>
        public short? lb_weight { get; set; }
    }
}
