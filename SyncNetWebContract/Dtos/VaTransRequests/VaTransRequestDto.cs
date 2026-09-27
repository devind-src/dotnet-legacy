using System;

namespace SyncNetApi.Dtos.VaTransRequests
{
    /// <summary>Row shape shared by Topup/Adjustment/Approval lists and single-record GET —
    /// mirrors legacy's VwTransRequest (va_trans_request joined to va_account for VaName/
    /// GroupName display).</summary>
    public record VaTransRequestDto(
        int TranNr,
        string? AccNr,
        string? VaName,
        string? GroupName,
        long Amount,
        string? TranType,
        string? TranTypeName,
        string? TraceNr,
        string? DebetCredit,
        string? ReffNr,
        string? Description,
        DateTime? RequestDate,
        DateTime? ApprovalDate,
        string? UsrName,
        string? SpvName,
        string? Status,
        string? StatusName);
}
