using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductPromos;

namespace SyncNetApi.Services.ProductPromos
{
    public interface IProductPromoService
    {
        Task<IReadOnlyList<ProductPromoDto>> GetRecordsAsync(string? filter = null);
        Task<ProductPromoDto?> GetByIdAsync(long id);
        Task<ProductPromoDto> CreateAsync(CreateProductPromoRequest request, string actingUser);
        Task<ProductPromoDto> UpdateAsync(long id, UpdateProductPromoRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
