using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteMargins;

namespace SyncNetApi.Services.RouteMargins
{
    public interface IRouteMarginService
    {
        Task<IReadOnlyList<RouteMarginDto>> GetRecordsAsync(string? filter = null);
        Task<RouteMarginDto?> GetByIdAsync(string instId);
        Task<RouteMarginDto> CreateAsync(CreateRouteMarginRequest request, string actingUser);
        Task<RouteMarginDto> UpdateAsync(string instId, UpdateRouteMarginRequest request, string actingUser);
        Task DeleteAsync(string instId, string actingUser);
        Task<IReadOnlyList<RouteMarginDto>> SyncCategoryAsync(SyncRouteMarginRequest request, string actingUser);
    }
}
