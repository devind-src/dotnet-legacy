using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_merchant_criteria" — menu "Product &gt; Pricing &amp; Fees &gt; QRIS Fees"
    /// (`/product/pricing-fees/qris-fees`). Originally added read-only in Phase 4 (`id`+`criteria` only,
    /// backing Terminal &gt; Group's "Criteria" dropdown via `LookupsController.GetMerchantCriteria`
    /// — that endpoint still exists and is untouched); now upgraded to full CRUD here.
    /// `id` (bigint) **is** `GENERATED ALWAYS AS IDENTITY` (verified live) — do not assign it
    /// manually, unlike most other manual-PK tables in this project. `criteria` is immutable
    /// after create (legacy renders it readonly on edit). Widths verified live: criteria
    /// varchar(30), notes/group_name/subgroup_name varchar(50), all fee columns
    /// numeric(12,2).</summary>
    [Table("sw_merchant_criteria", Schema = "public")]
    public class SwMerchantCriteria
    {
        [Key]
        public long id { get; set; }
        public string criteria { get; set; } = "";
        public decimal mdr { get; set; }
        public decimal fee_from_issuer { get; set; }
        public decimal fee_from_partner { get; set; }
        public decimal fee_to_merchant { get; set; }
        public decimal fee_to_submerchant { get; set; }
        public decimal fee_to_switch { get; set; }
        public string notes { get; set; } = "";
        public string group_name { get; set; } = "";
        public string subgroup_name { get; set; } = "";
    }
}
