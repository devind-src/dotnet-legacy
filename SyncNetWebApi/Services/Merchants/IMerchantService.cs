using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Merchants;

namespace SyncNetApi.Services.Merchants
{
    public interface IMerchantService
    {
        Task<IReadOnlyList<MerchantDto>> GetRecordsAsync(string? filter = null);
        Task<MerchantDto?> GetByIdAsync(string merchantId);
        Task<MerchantDto> CreateAsync(CreateMerchantRequest request, string actingUser);
        Task<MerchantDto> UpdateAsync(string merchantId, UpdateMerchantRequest request, string actingUser);
        Task DeleteAsync(string merchantId, string actingUser);
    }
}
