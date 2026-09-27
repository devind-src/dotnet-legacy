using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Connections;

namespace SyncNetApi.Services.Connections
{
    public interface IConnectionService
    {
        Task<IReadOnlyList<ConnectionDto>> GetByNodeIdAsync(int nodeId);
        Task<ConnectionDto?> GetByIdAsync(string connName);
        Task<ConnectionDto> CreateAsync(CreateConnectionRequest request, string actingUser);
        Task<ConnectionDto> UpdateAsync(string connName, UpdateConnectionRequest request, string actingUser);
        Task DeleteAsync(string connName, string actingUser);
    }
}
