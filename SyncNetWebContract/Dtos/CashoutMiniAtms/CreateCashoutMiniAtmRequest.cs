using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CashoutMiniAtms
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_cashout_bank.id is not identity). AccName is intentionally not [Required] — legacy
    /// IsValid() never checks it for presence.</summary>
    public class CreateCashoutMiniAtmRequest
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
