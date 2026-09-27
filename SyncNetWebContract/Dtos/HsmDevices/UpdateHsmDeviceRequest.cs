using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmDevices
{
    /// <summary>HsmName is immutable after create — only these fields can change.</summary>
    public class UpdateHsmDeviceRequest
    {
        [Range(0, 10)]
        public short? Priority { get; set; }

        [Required, RegularExpression("^[12]$", ErrorMessage = "Protocol must be 1 (TCP/IP) or 2 (Emulator).")]
        public string Protocol { get; set; } = "2";

        public bool UseScheme { get; set; } = true;

        [Range(0, 16)]
        public short? MessageHeader { get; set; }

        [MaxLength(15)]
        public string? RemoteIp { get; set; }

        [MaxLength(5)]
        public string? RemotePort { get; set; }
    }
}
