using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Tools
{
    /// <summary>Value/Key must be hex and exactly 16/32/48 characters (DES/2-key 3DES/3-key
    /// 3DES) — validated in the service layer (matches legacy DesCalculator.Validate()).</summary>
    public class DesCalculatorRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty;
    }
}
