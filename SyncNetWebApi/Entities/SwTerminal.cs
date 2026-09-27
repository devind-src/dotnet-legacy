using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_terminal", Schema = "public")]
    public class SwTerminal
    {
        [Key]
        public string term_id { get; set; } = string.Empty;
        public int? team_id { get; set; }
        public string? merchant_id { get; set; }
        public string? store_id { get; set; }
        public string? location { get; set; }
        public string? brand { get; set; }
        public string? type { get; set; }
        public string? serial_number { get; set; }
        public string? app_version { get; set; }
        public string? inst_date { get; set; }
        public DateTime? last_trx_date { get; set; }
        public DateTime? last_msg_date { get; set; }
        public string? virtual_account { get; set; }
        public string? acc_number { get; set; }
        public string? security { get; set; }
        public string? va_name { get; set; }
        public string? ip_address { get; set; }
        public string? mac_address { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
        public string? loket_name { get; set; }
        public string? city { get; set; }
        public string? phone { get; set; }
        public string? group_name { get; set; }
        public string? submerchant_id { get; set; }
        public string? subgroup_name { get; set; }
        public string? bank_name { get; set; }
        public string? bank_branch { get; set; }
        public string? bank_code { get; set; }
        public string? bank_acc_number { get; set; }
        public string? bank_acc_name { get; set; }
        public string? nmid { get; set; }
        public decimal? va_balance { get; set; }
        public string? chat_id { get; set; }
        public string? criteria { get; set; }
        public string? mpan { get; set; }
        public string? city_code { get; set; }
    }
}
