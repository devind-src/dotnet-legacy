using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_merchant_product" — menu "Product &gt; Master &gt; Merchant" (`/product/master/merchant`).
    /// `id` (int) was manually computed as max(id)+1 (legacy quirk), same pattern as
    /// Bank/Brand/RouteSource; converted to a real Postgres IDENTITY column — DB now assigns it
    /// on insert. No FK constraints exist to sw_merchant/sw_product;
    /// legacy validates existence of both manually before insert/update (replicated in the
    /// service layer). The real table also has `category`/`last_update`/`update_by` columns,
    /// but the legacy C# model never mapped them and no legacy form ever wrote them — left
    /// unmapped here too (the "Category" column shown in the legacy list grid is actually the
    /// joined Product Master's category, not this table's own unused column). Widths verified
    /// live: merchant_id varchar(15), product_id varchar(50), notes varchar(50).</summary>
    [Table("sw_merchant_product", Schema = "public")]
    public class SwMerchantProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string? merchant_id { get; set; }
        public string? product_id { get; set; }
        public string? notes { get; set; }
        public string? status { get; set; }
    }
}
