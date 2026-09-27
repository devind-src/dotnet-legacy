using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TerminalLimits
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1 (see
    /// SwTerminalLimit entity note).</summary>
    public class CreateTerminalLimitRequest
    {
        [Required, MaxLength(30)]
        public string LimitName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? GroupName { get; set; }

        public long MinWithdrawal { get; set; }
        public long MaxWithdrawal { get; set; }
        public long MinTransfer { get; set; }
        public long MaxTransfer { get; set; }
        public long MinPurchase { get; set; }
        public long MaxPurchase { get; set; }
        public long MinPayment { get; set; }
        public long MaxPayment { get; set; }
    }
}
