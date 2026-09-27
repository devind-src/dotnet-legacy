using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.AgentBanks;

namespace SyncNetApi.Services.AgentBanks
{
    public interface IAgentBankService
    {
        Task<IReadOnlyList<AgentBankDto>> GetRecordsAsync(string? filter = null);
        Task<AgentBankDto?> GetByIdAsync(long id);
        Task<AgentBankDto> CreateAsync(CreateAgentBankRequest request, string actingUser);
        Task<AgentBankDto> UpdateAsync(long id, UpdateAgentBankRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
