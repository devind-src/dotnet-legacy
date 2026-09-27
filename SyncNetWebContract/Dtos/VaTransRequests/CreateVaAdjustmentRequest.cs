using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaTransRequests
{
    /// <summary>TranType/TraceNr/ReffNr/Status are not supplied by the caller — computed
    /// server-side from DebetCredit (tran_type="91" Debet / "90" Credit, random trace/reff
    /// numbers, status="0"/Request), mirroring legacy Adjustment Detail.razor's Save().</summary>
    public class CreateVaAdjustmentRequest
    {
        [Required, MaxLength(30)]
        public string AccNr { get; set; } = string.Empty;

        /// <summary>"D" (Debet) or "C" (Credit).</summary>
        [Required, RegularExpression("^[DC]$", ErrorMessage = "Tran type must be D or C")]
        public string DebetCredit { get; set; } = "D";

        [Range(1, long.MaxValue, ErrorMessage = "Amount adjustment mandatory")]
        public long Amount { get; set; }

        [Required, MaxLength(50)]
        public string Description { get; set; } = string.Empty;
    }
}
