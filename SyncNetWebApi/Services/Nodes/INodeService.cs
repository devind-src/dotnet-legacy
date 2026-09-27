using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Nodes;

namespace SyncNetApi.Services.Nodes
{
    public interface INodeService
    {
        Task<IReadOnlyList<NodeDto>> GetRecordsAsync(string? filter = null);
        Task<NodeDto?> GetByIdAsync(int nodeId);
        Task<(string PortIn, string PortOut)> GetNewPortsAsync();
        Task<NodeDto> CreateAsync(CreateNodeRequest request, string actingUser);
        Task<NodeDto> UpdateAsync(int nodeId, UpdateNodeRequest request, string actingUser);
        Task DeleteAsync(int nodeId, string actingUser);
    }
}
