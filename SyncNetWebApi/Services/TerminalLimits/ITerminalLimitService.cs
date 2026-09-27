using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.TerminalLimits;

namespace SyncNetApi.Services.TerminalLimits
{
    public interface ITerminalLimitService
    {
        Task<IReadOnlyList<TerminalLimitDto>> GetRecordsAsync(string? filter = null);
        Task<TerminalLimitDto?> GetByIdAsync(long id);
        Task<TerminalLimitDto> CreateAsync(CreateTerminalLimitRequest request, string actingUser);
        Task<TerminalLimitDto> UpdateAsync(long id, UpdateTerminalLimitRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
