using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CashoutWithdrawals
{
    /// <summary>Every field is editable on Update — legacy TerminalBankUpdate does a full
    /// CurrentValues.SetValues() replace, including terminal_id/merchant_id (no field is
    /// readonly on the legacy Edit form). Duplicate-combo check is intentionally NOT re-run
    /// here — legacy only runs it on Create.</summary>
    public class UpdateCashoutWithdrawalRequest
    {
        [Required, MaxLength(16)]
        public string TerminalId { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string MerchantId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string AccNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? AccName { get; set; }
    }
}
