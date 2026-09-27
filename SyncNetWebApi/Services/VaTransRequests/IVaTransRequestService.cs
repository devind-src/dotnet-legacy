using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.VaTransRequests;

namespace SyncNetApi.Services.VaTransRequests
{
    public interface IVaTransRequestService
    {
        Task<IReadOnlyList<VaTransRequestDto>> GetTopupRecordsAsync(string? filter = null);
        Task<IReadOnlyList<VaTransRequestDto>> GetAdjustmentRecordsAsync(string? filter = null);
        Task<IReadOnlyList<VaTransRequestDto>> GetApprovalRecordsAsync(string? filter = null);
        Task<VaTransRequestDto?> GetByIdAsync(int tranNr);
        Task<VaTransRequestDto> CreateTopupAsync(CreateVaTopupRequest request, string actingUser);
        Task<VaTransRequestDto> CreateAdjustmentAsync(CreateVaAdjustmentRequest request, string actingUser);
        Task<VaTransRequestDto> ApproveAsync(int tranNr, string actingUser);
        Task<VaTransRequestDto> RejectAsync(int tranNr, string actingUser);
    }
}
