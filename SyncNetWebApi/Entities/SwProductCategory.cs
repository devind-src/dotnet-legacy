using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_product_category" — menu "Product &gt; Master &gt; Category" (`/product/master/category`).
    /// Originally added read-only in Phase R just to back Routing &gt; Dynamic's "topup
    /// products" dropdown filter (`is_topup = "1"`); now upgraded to full CRUD in Phase P1.
    /// `category` is a manual string PK (varchar(30)), immutable after create. Widths
    /// verified live: category varchar(30), notes varchar(50), is_topup varchar(2).
    /// `updated_by`/`updated_dt` exist on the real table but the legacy C# model never mapped
    /// them and no legacy form ever wrote them — mapped here only so they're visible if ever
    /// populated by something else, never written by Create/Update.</summary>
    [Table("sw_product_category", Schema = "public")]
    public class SwProductCategory
    {
        [Key]
        [MaxLength(30)]
        public string category { get; set; } = string.Empty;
        public string? notes { get; set; }
        public string? is_topup { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
