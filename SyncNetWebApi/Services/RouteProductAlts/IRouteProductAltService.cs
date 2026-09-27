using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteProductAlts;

namespace SyncNetApi.Services.RouteProductAlts
{
    public interface IRouteProductAltService
    {
        Task<IReadOnlyList<RouteProductAltDto>> GetByProductAsync(string instId);
        Task<RouteProductAltDto?> GetByIdAsync(int id);
        Task<RouteProductAltDto> CreateAsync(CreateRouteProductAltRequest request, string actingUser);
        Task<RouteProductAltDto> UpdateAsync(int id, UpdateRouteProductAltRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
