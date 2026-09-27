using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_merchant_group", Schema = "public")]
    public class SwMerchantGroup
    {
        [Key]
        public string group_name { get; set; } = string.Empty;
        public string? participant_id { get; set; }
        public string? notes { get; set; }
    }
}
