using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.SupportTeams;

namespace SyncNetApi.Services.SupportTeams
{
    public interface ISupportTeamService
    {
        Task<IReadOnlyList<SupportTeamDto>> GetRecordsAsync(string? filter = null);
        Task<SupportTeamDto?> GetByIdAsync(int teamId);
        Task<SupportTeamDto> CreateAsync(CreateSupportTeamRequest request, string actingUser);
        Task<SupportTeamDto> UpdateAsync(int teamId, UpdateSupportTeamRequest request, string actingUser);
        Task DeleteAsync(int teamId, string actingUser);
    }
}
