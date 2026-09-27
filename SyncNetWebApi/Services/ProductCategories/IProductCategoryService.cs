using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductCategories;

namespace SyncNetApi.Services.ProductCategories
{
    public interface IProductCategoryService
    {
        Task<IReadOnlyList<ProductCategoryDto>> GetRecordsAsync(string? filter = null);
        Task<ProductCategoryDto?> GetByIdAsync(string category);
        Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, string actingUser);
        Task<ProductCategoryDto> UpdateAsync(string category, UpdateProductCategoryRequest request, string actingUser);
        Task DeleteAsync(string category, string actingUser);
    }
}
