using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.VaGroups;

namespace SyncNetApi.Services.VaGroups
{
    public interface IVaGroupService
    {
        Task<IReadOnlyList<VaGroupDto>> GetRecordsAsync(string? filter = null);
        Task<VaGroupDto?> GetByIdAsync(string groupName);
        Task<VaGroupDto> CreateAsync(CreateVaGroupRequest request, string actingUser);
        Task<VaGroupDto> UpdateAsync(string groupName, UpdateVaGroupRequest request, string actingUser);
        Task DeleteAsync(string groupName, string actingUser);
    }
}
