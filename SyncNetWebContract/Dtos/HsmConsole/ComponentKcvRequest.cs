using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmConsole
{
    /// <summary>"Form a Key" tab's per-component Check button — InputMode is "clear" (default)
    /// or "encrypted" (the component is already wrapped under LMK, e.g. pasted from a stored
    /// ZMK value).</summary>
    public class ComponentKcvRequest
    {
        [Required]
        public string Component { get; set; } = string.Empty;

        public string InputMode { get; set; } = "clear";
    }
}
