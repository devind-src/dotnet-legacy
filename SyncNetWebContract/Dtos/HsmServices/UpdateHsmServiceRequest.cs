using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmServices
{
    /// <summary>HsmDesc is immutable after create — only these fields can change.</summary>
    public class UpdateHsmServiceRequest
    {
        [Required, Range(1, 65000)]
        public int HsmPort { get; set; }

        [Required, Range(1, 120)]
        public int RequestTimeout { get; set; }

        public short? MessageHeader { get; set; }
    }
}
