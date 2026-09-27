using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmServices
{
    /// <summary>hsm_desc is varchar(50) — real column width. Validation mirrors legacy
    /// IsValid(): port 1-65000, request timeout 1-120.</summary>
    public class CreateHsmServiceRequest
    {
        [Required, MaxLength(50)]
        public string HsmDesc { get; set; } = string.Empty;

        [Required, Range(1, 65000)]
        public int HsmPort { get; set; }

        [Required, Range(1, 120)]
        public int RequestTimeout { get; set; }

        public short? MessageHeader { get; set; }
    }
}
