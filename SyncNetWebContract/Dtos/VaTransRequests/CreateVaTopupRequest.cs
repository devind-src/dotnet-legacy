using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaTransRequests
{
    /// <summary>TranType/DebetCredit/TraceNr/ReffNr/Status are not supplied by the caller —
    /// computed server-side (tran_type="80", debet_credit="C", random trace/reff numbers,
    /// status="0"/Request), mirroring legacy Topup Detail.razor's Save().</summary>
    public class CreateVaTopupRequest
    {
        [Required, MaxLength(30)]
        public string AccNr { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "Amount topup mandatory")]
        public long Amount { get; set; }

        [Required, MaxLength(50)]
        public string Description { get; set; } = string.Empty;
    }
}
