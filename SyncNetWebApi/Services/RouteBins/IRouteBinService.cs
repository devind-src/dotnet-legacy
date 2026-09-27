using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteBins;

namespace SyncNetApi.Services.RouteBins
{
    public interface IRouteBinService
    {
        Task<IReadOnlyList<RouteBinDto>> GetRecordsAsync(string? filter = null);
        Task<RouteBinDto?> GetByIdAsync(int groupId);
        Task<RouteBinDto> CreateAsync(CreateRouteBinRequest request, string actingUser);
        Task<RouteBinDto> UpdateAsync(int groupId, UpdateRouteBinRequest request, string actingUser);
        Task DeleteAsync(int groupId, string actingUser);
    }
}
