using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_fee_additional" — menu "Product &gt; Pricing &amp; Fees &gt; Additional Fees"
    /// (`/product/pricing-fees/additional-fees`, "Additional Fee for Withdrawal" in legacy). `id` (bigint)
    /// is `GENERATED ALWAYS AS IDENTITY` (verified live) — do not assign manually.
    /// `group_name` (Card/Merchant Group name, free text from a datalist) is immutable after
    /// create. Width verified live: group_name varchar(50).</summary>
    [Table("sw_fee_additional", Schema = "public")]
    public class SwFeeAdditional
    {
        [Key]
        public long id { get; set; }
        public string? group_name { get; set; }
        public int fee { get; set; }
    }
}
