using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductVolumeTierings;

namespace SyncNetApi.Services.ProductVolumeTierings
{
    public interface IProductVolumeTieringService
    {
        Task<IReadOnlyList<ProductVolumeTieringDto>> GetRecordsAsync(string? filter = null);
        Task<ProductVolumeTieringDto?> GetByIdAsync(long id);
        Task<ProductVolumeTieringDto> CreateAsync(CreateProductVolumeTieringRequest request, string actingUser);
        Task<ProductVolumeTieringDto> UpdateAsync(long id, UpdateProductVolumeTieringRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
