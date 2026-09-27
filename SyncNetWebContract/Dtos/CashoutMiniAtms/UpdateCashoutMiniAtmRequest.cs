using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CashoutMiniAtms
{
    /// <summary>Every field is editable on Update — legacy CashoutBankUpdate does a full
    /// CurrentValues.SetValues() replace, including group_name (no field is readonly on the
    /// legacy Edit form). Duplicate-combo check is intentionally NOT re-run here — legacy only
    /// runs it on Create.</summary>
    public class UpdateCashoutMiniAtmRequest
    {
        [Required, MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string AccNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? AccName { get; set; }
    }
}
