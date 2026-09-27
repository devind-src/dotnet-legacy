using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteFailoverConfigs;

namespace SyncNetApi.Services.RouteFailoverConfigs
{
    public interface IRouteFailoverConfigService
    {
        Task<IReadOnlyList<RouteFailoverConfigDto>> GetRecordsAsync(string? filter = null);
        Task<RouteFailoverConfigDto?> GetByIdAsync(int id);
        Task<RouteFailoverConfigDto> CreateAsync(CreateRouteFailoverConfigRequest request, string actingUser);
        Task<RouteFailoverConfigDto> UpdateAsync(int id, UpdateRouteFailoverConfigRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
        Task<IReadOnlyList<FailoverSwitchDto>> GetSwitchesAsync();
        Task<FailoverSwitchDto> SetSwitchAsync(string routingType, bool enabled, string actingUser);
    }
}
