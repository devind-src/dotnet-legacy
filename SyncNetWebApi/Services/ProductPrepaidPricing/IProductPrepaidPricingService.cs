using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductPrepaidPricing;

namespace SyncNetApi.Services.ProductPrepaidPricing
{
    public interface IProductPrepaidPricingService
    {
        Task<IReadOnlyList<ProductPrepaidPricingDto>> GetRecordsAsync(string? filter = null);
        Task<ProductPrepaidPricingDto?> GetByIdAsync(int id);
        Task<ProductPrepaidPricingDto> CreateAsync(CreateProductPrepaidPricingRequest request, string actingUser);
        Task<ProductPrepaidPricingDto> UpdateAsync(int id, UpdateProductPrepaidPricingRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);

        /// <summary>Master: produk topup yang punya harga supplier atau route margin, beserta Routing Mode.</summary>
        Task<IReadOnlyList<TopupRoutingDto>> GetTopupRoutingAsync(string? filter = null);
        Task<TopupRoutingDto> UpdateTopupRoutingAsync(string productId, UpdateTopupRoutingRequest request, string actingUser);
        /// <summary>Salin priority dan LB weight baris ini ke semua denom supplier yang sama pada produk ini.</summary>
        Task<IReadOnlyList<ProductPrepaidPricingDto>> ApplyToAllDenomsAsync(int id, string actingUser);
        /// <summary>LB weight semua supplier satu produk + denom; total supplier aktif harus 100.</summary>
        Task<IReadOnlyList<ProductPrepaidPricingDto>> UpdateWeightsAsync(string productId, UpdateSupplierWeightsRequest request, string actingUser);
    }
}
