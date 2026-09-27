using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductSupplierPrices;

namespace SyncNetApi.Services.ProductSupplierPrices
{
    public interface IProductSupplierPriceService
    {
        Task<IReadOnlyList<ProductSupplierPriceDto>> GetRecordsAsync(string? filter = null);
        Task<ProductSupplierPriceDto?> GetByIdAsync(int id);
        Task<ProductSupplierPriceDto> CreateAsync(CreateProductSupplierPriceRequest request, string actingUser);
        Task<ProductSupplierPriceDto> UpdateAsync(int id, UpdateProductSupplierPriceRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
