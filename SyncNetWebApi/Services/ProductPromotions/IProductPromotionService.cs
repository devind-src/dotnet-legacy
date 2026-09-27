using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductPromotions;

namespace SyncNetApi.Services.ProductPromotions
{
    public interface IProductPromotionService
    {
        Task<IReadOnlyList<ProductPromotionDto>> GetRecordsAsync(string? filter = null);
        Task<ProductPromotionDto?> GetByIdAsync(long id);
        Task<ProductPromotionDto> CreateAsync(CreateProductPromotionRequest request, string actingUser);
        Task<ProductPromotionDto> UpdateAsync(long id, UpdateProductPromotionRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
