using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.SubMerchantGroups;

namespace SyncNetApi.Services.SubMerchantGroups
{
    public interface ISubMerchantGroupService
    {
        Task<IReadOnlyList<SubMerchantGroupDto>> GetRecordsAsync(string? filter = null);
        Task<SubMerchantGroupDto?> GetByNameAsync(string groupName);
        Task<SubMerchantGroupDto> CreateAsync(CreateSubMerchantGroupRequest request, string actingUser);
        Task<SubMerchantGroupDto> UpdateAsync(string groupName, UpdateSubMerchantGroupRequest request, string actingUser);
        Task DeleteAsync(string groupName, string actingUser);
    }
}
