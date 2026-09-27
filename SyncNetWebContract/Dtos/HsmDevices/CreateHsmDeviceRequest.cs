using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmDevices
{
    /// <summary>hsm_name is varchar(25) — real column width. Protocol only allows "1" (TCP/IP)
    /// or "2" (Emulator) — legacy's "0" (COMM) is dropped from this app entirely (dead feature,
    /// never implemented even in the legacy form). Priority is 0-10, matching the legacy form's
    /// range — purely descriptive metadata, not wired to any load-balancing/routing logic (HSM
    /// math always runs the local Emulator directly; multi-device routing was explicitly
    /// dropped from scope since this dashboard only does generate/encrypt, not the
    /// transaction-time key translate that would need it).</summary>
    public class CreateHsmDeviceRequest
    {
        [Required, MaxLength(25)]
        public string HsmName { get; set; } = string.Empty;

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
