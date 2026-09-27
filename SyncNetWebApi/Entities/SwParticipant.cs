using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_participant", Schema = "public")]
    public class SwParticipant
    {
        [Key]
        public string participant_id { get; set; } = string.Empty;
        public string? inst_id { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? zipcode { get; set; }
        public string? person { get; set; }
        public string? phone { get; set; }
        public string? fax { get; set; }
        public string? email { get; set; }
        public string? virtual_account { get; set; }
        public string? acc_number { get; set; }
        public string? active { get; set; }
        public string? va_name { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
