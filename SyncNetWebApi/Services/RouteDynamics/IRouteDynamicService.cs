using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteDynamics;

namespace SyncNetApi.Services.RouteDynamics
{
    public interface IRouteDynamicService
    {
        Task<IReadOnlyList<RouteDynamicDto>> GetRecordsAsync(string? filter = null);
        Task<RouteDynamicDto?> GetByIdAsync(string instId);
        Task<RouteDynamicDto> CreateAsync(CreateRouteDynamicRequest request, string actingUser);
        Task<RouteDynamicDto> UpdateAsync(string instId, UpdateRouteDynamicRequest request, string actingUser);
        Task DeleteAsync(string instId, string actingUser);
    }
}
