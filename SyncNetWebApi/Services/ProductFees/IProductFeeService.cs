using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductFees;

namespace SyncNetApi.Services.ProductFees
{
    public interface IProductFeeService
    {
        Task<IReadOnlyList<ProductFeeDto>> GetRecordsAsync(string? filter = null);
        Task<ProductFeeDto?> GetByIdAsync(long id);
        Task<ProductFeeDto> CreateAsync(CreateProductFeeRequest request, string actingUser);
        Task<ProductFeeDto> UpdateAsync(long id, UpdateProductFeeRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
