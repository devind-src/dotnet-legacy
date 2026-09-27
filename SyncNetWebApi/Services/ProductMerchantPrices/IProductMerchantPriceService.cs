using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductMerchantPrices;

namespace SyncNetApi.Services.ProductMerchantPrices
{
    public interface IProductMerchantPriceService
    {
        Task<IReadOnlyList<ProductMerchantPriceDto>> GetRecordsAsync(string? filter = null);
        Task<ProductMerchantPriceDto?> GetByIdAsync(int id);
        Task<ProductMerchantPriceDto> CreateAsync(CreateProductMerchantPriceRequest request, string actingUser);
        Task<ProductMerchantPriceDto> UpdateAsync(int id, UpdateProductMerchantPriceRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
