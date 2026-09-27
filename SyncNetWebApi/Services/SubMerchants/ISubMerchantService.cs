using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.SubMerchants;

namespace SyncNetApi.Services.SubMerchants
{
    public interface ISubMerchantService
    {
        Task<IReadOnlyList<SubMerchantDto>> GetRecordsAsync(string? filter = null);
        Task<SubMerchantDto?> GetByIdAsync(string subMerchantId);
        Task<SubMerchantDto> CreateAsync(CreateSubMerchantRequest request, string actingUser);
        Task<SubMerchantDto> UpdateAsync(string subMerchantId, UpdateSubMerchantRequest request, string actingUser);
        Task DeleteAsync(string subMerchantId, string actingUser);
    }
}
