using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Apps;

namespace SyncNetApi.Services.Apps
{
    public interface IAppService
    {
        Task<IReadOnlyList<AppDto>> GetRecordsAsync(string? filter = null);
        Task<AppDto?> GetByIdAsync(string appName);
        Task<int> GetNewCommandPortAsync();
        Task<AppDto> CreateAsync(CreateAppRequest request, string actingUser);
        Task<AppDto> UpdateAsync(string appName, UpdateAppRequest request, string actingUser);
        Task DeleteAsync(string appName, string actingUser);
    }
}
