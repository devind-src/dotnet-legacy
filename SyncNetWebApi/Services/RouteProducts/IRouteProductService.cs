using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteProducts;

namespace SyncNetApi.Services.RouteProducts
{
    public interface IRouteProductService
    {
        Task<IReadOnlyList<RouteProductDto>> GetRecordsAsync(string? filter = null);
        Task<RouteProductDto?> GetByIdAsync(string instId);
        Task<RouteProductDto> CreateAsync(CreateRouteProductRequest request, string actingUser);
        Task<RouteProductDto> UpdateAsync(string instId, UpdateRouteProductRequest request, string actingUser);
        Task DeleteAsync(string instId, string actingUser);
    }
}
