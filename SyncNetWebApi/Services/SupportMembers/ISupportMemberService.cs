using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.SupportMembers;

namespace SyncNetApi.Services.SupportMembers
{
    public interface ISupportMemberService
    {
        Task<IReadOnlyList<SupportMemberDto>> GetRecordsAsync(string? filter = null);
        Task<SupportMemberDto?> GetByIdAsync(int memberId);
        Task<SupportMemberDto> CreateAsync(CreateSupportMemberRequest request, string actingUser);
        Task<SupportMemberDto> UpdateAsync(int memberId, UpdateSupportMemberRequest request, string actingUser);
        Task DeleteAsync(int memberId, string actingUser);
    }
}
