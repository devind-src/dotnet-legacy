using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Terminals;

namespace SyncNetApi.Services.Terminals
{
    public interface ITerminalService
    {
        Task<IReadOnlyList<TerminalDto>> GetRecordsAsync(string? filter = null);
        Task<TerminalDto?> GetByIdAsync(string termId);
        Task<TerminalDto> CreateAsync(CreateTerminalRequest request, string actingUser);
        Task<TerminalDto> UpdateAsync(string termId, UpdateTerminalRequest request, string actingUser);
        Task DeleteAsync(string termId, string actingUser);
    }
}
