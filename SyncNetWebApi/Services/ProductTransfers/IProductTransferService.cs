using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductTransfers;

namespace SyncNetApi.Services.ProductTransfers
{
    public interface IProductTransferService
    {
        Task<IReadOnlyList<ProductTransferDto>> GetRecordsAsync(string? filter = null);
        Task<ProductTransferDto?> GetByIdAsync(long id);
        Task<ProductTransferDto> CreateAsync(CreateProductTransferRequest request, string actingUser);
        Task<ProductTransferDto> UpdateAsync(long id, UpdateProductTransferRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
