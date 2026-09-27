using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Products;

namespace SyncNetApi.Services.Products
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductDto>> GetRecordsAsync(string? filter = null);
        Task<ProductDto?> GetByIdAsync(string productCode);
        Task<bool> ExistsAsync(string productCode);
        Task<ProductDto> CreateAsync(CreateProductRequest request, string actingUser);
        Task<ProductDto> UpdateAsync(string productCode, UpdateProductRequest request, string actingUser);
        Task DeleteAsync(string productCode, string actingUser);
    }
}
