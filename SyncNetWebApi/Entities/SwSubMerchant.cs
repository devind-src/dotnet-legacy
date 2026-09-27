using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_submerchant", Schema = "public")]
    public class SwSubMerchant
    {
        [Key]
        public string submerchant_id { get; set; } = string.Empty;
        public string? merchant_id { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? zipcode { get; set; }
        public string? phone { get; set; }
        public string? fax { get; set; }
        public string? email { get; set; }
        public string? pic { get; set; }
        public string? date_join { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
        public string? submerchant_id_ext { get; set; }
    }
}
