using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.MerchantGroups;

namespace SyncNetApi.Services.MerchantGroups
{
    public interface IMerchantGroupService
    {
        Task<IReadOnlyList<MerchantGroupDto>> GetRecordsAsync(string? filter = null);
        Task<MerchantGroupDto?> GetByNameAsync(string groupName);
        Task<MerchantGroupDto> CreateAsync(CreateMerchantGroupRequest request, string actingUser);
        Task<MerchantGroupDto> UpdateAsync(string groupName, UpdateMerchantGroupRequest request, string actingUser);
        Task DeleteAsync(string groupName, string actingUser);
    }
}
