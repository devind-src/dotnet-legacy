using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductAdditionalFees;

namespace SyncNetApi.Services.ProductAdditionalFees
{
    public interface IProductAdditionalFeeService
    {
        Task<IReadOnlyList<ProductAdditionalFeeDto>> GetRecordsAsync(string? filter = null);
        Task<ProductAdditionalFeeDto?> GetByIdAsync(long id);
        Task<ProductAdditionalFeeDto> CreateAsync(CreateProductAdditionalFeeRequest request, string actingUser);
        Task<ProductAdditionalFeeDto> UpdateAsync(long id, UpdateProductAdditionalFeeRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
