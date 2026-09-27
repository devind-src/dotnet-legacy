using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductMappings;

namespace SyncNetApi.Services.ProductMappings
{
    public interface IProductMappingService
    {
        Task<IReadOnlyList<ProductMappingDto>> GetRecordsAsync(string? filter = null);
        Task<ProductMappingDto?> GetByIdAsync(long id);
        Task<ProductMappingDto> CreateAsync(CreateProductMappingRequest request, string actingUser);
        Task<ProductMappingDto> UpdateAsync(long id, UpdateProductMappingRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
