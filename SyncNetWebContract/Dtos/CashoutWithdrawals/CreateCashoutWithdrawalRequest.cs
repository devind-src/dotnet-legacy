using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CashoutWithdrawals
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_terminal_bank.id is not identity). AccName is intentionally not [Required] — legacy
    /// IsValid() never checks it for presence.</summary>
    public class CreateCashoutWithdrawalRequest
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
