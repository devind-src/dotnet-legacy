using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductMerchantPricing;

namespace SyncNetApi.Services.ProductMerchantPricing
{
    public interface IProductMerchantPricingService
    {
        Task<IReadOnlyList<ProductMerchantPricingDto>> GetRecordsAsync(string? filter = null);
        Task<ProductMerchantPricingDto?> GetByIdAsync(int id);
        Task<ProductMerchantPricingDto> CreateAsync(CreateProductMerchantPricingRequest request, string actingUser);
        Task<ProductMerchantPricingDto> UpdateAsync(int id, UpdateProductMerchantPricingRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
