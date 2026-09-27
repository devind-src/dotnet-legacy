using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.TerminalClients;

namespace SyncNetApi.Services.TerminalClients
{
    public interface ITerminalClientService
    {
        Task<IReadOnlyList<TerminalClientDto>> GetRecordsAsync(string? filter = null);
        Task<TerminalClientDto?> GetByIdAsync(string clientId);
        Task<TerminalClientDto> CreateAsync(CreateTerminalClientRequest request, string actingUser);
        Task<TerminalClientDto> UpdateAsync(string clientId, UpdateTerminalClientRequest request, string actingUser);
        Task DeleteAsync(string clientId, string actingUser);
    }
}
