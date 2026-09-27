using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductFeeAdditionals;

namespace SyncNetApi.Services.ProductFeeAdditionals
{
    public interface IProductFeeAdditionalService
    {
        Task<IReadOnlyList<ProductFeeAdditionalDto>> GetRecordsAsync(string? filter = null);
        Task<ProductFeeAdditionalDto?> GetByIdAsync(long id);
        Task<ProductFeeAdditionalDto> CreateAsync(CreateProductFeeAdditionalRequest request, string actingUser);
        Task<ProductFeeAdditionalDto> UpdateAsync(long id, UpdateProductFeeAdditionalRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
