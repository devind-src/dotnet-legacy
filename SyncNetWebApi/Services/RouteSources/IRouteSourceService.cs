using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteSources;

namespace SyncNetApi.Services.RouteSources
{
    public interface IRouteSourceService
    {
        Task<IReadOnlyList<RouteSourceDto>> GetRecordsAsync(string? filter = null);
        Task<RouteSourceDto?> GetByIdAsync(int id);
        Task<RouteSourceDto> CreateAsync(CreateRouteSourceRequest request, string actingUser);
        Task<RouteSourceDto> UpdateAsync(int id, UpdateRouteSourceRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
