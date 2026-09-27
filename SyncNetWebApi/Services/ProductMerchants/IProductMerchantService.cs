using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductMerchants;

namespace SyncNetApi.Services.ProductMerchants
{
    public interface IProductMerchantService
    {
        Task<IReadOnlyList<ProductMerchantDto>> GetRecordsAsync(string? filter = null);
        Task<ProductMerchantDto?> GetByIdAsync(int id);
        Task<ProductMerchantDto> CreateAsync(CreateProductMerchantRequest request, string actingUser);
        Task<ProductMerchantDto> UpdateAsync(int id, UpdateProductMerchantRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
