using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_merchant", Schema = "public")]
    public class SwMerchant
    {
        [Key]
        public string merchant_id { get; set; } = string.Empty;
        public string? participant_id { get; set; }
        public string? mcc_code { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? zipcode { get; set; }
        public string? phone { get; set; }
        public string? fax { get; set; }
        public string? email { get; set; }
        public string? person { get; set; }
        public string? bank_name { get; set; }
        public string? branch { get; set; }
        public string? dest_bank { get; set; }
        public string? dest_acc { get; set; }
        public string? owner_name { get; set; }
        public string? contact { get; set; }
        public string? online_settlement { get; set; }
        public string? virtual_account { get; set; }
        public string? acc_number { get; set; }
        public string? status { get; set; }
        public string? va_name { get; set; }
        public string? date_join { get; set; }
        public string? brand { get; set; }
        public string? schedule_settlement { get; set; }
        public string? schedule_time { get; set; }
        public string? settlement_day { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
        public string? mid_ext { get; set; }
        public string? group_name { get; set; }
        public string? nmid { get; set; }
    }
}
