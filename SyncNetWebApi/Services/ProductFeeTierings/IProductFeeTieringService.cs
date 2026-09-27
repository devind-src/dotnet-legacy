using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductFeeTierings;

namespace SyncNetApi.Services.ProductFeeTierings
{
    public interface IProductFeeTieringService
    {
        Task<IReadOnlyList<ProductFeeTieringDto>> GetRecordsAsync(string? filter = null);
        Task<ProductFeeTieringDto?> GetByIdAsync(long id);
        Task<ProductFeeTieringDto> CreateAsync(CreateProductFeeTieringRequest request, string actingUser);
        Task<ProductFeeTieringDto> UpdateAsync(long id, UpdateProductFeeTieringRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
