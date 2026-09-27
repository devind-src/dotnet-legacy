using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductBins;

namespace SyncNetApi.Services.ProductBins
{
    public interface IProductBinService
    {
        Task<IReadOnlyList<ProductBinDto>> GetRecordsAsync(string? filter = null);
        Task<ProductBinDto?> GetByIdAsync(long id);
        Task<ProductBinDto> CreateAsync(CreateProductBinRequest request, string actingUser);
        Task<ProductBinDto> UpdateAsync(long id, UpdateProductBinRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
