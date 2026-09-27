using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_product_mapping" — menu "Product &gt; Master &gt; Mapping" (`/product/master/mapping`).
    /// `id` (bigint) was manually computed as max(id)+1 (legacy quirk), same pattern as
    /// Bank/Brand; converted to a real Postgres IDENTITY column — DB now assigns it on insert.
    /// The real table also has
    /// `category`/`is_topup`/`status`/`created_by/dt`/`updated_by/dt` columns, but the legacy
    /// C# model never mapped them and no legacy form ever wrote them — left unmapped here too.
    /// Widths verified live: app_name varchar(50), source_biller_code varchar(15),
    /// dest_biller_code varchar(30), notes varchar(50), is_deposit varchar(2).</summary>
    [Table("sw_product_mapping", Schema = "public")]
    public class SwProductMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? app_name { get; set; }
        public string? source_biller_code { get; set; }
        public int? denom { get; set; }
        public string? dest_biller_code { get; set; }
        public string? notes { get; set; }
        public string? is_deposit { get; set; }
    }
}
