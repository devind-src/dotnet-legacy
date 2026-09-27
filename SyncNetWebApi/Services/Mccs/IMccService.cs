using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Mccs;

namespace SyncNetApi.Services.Mccs
{
    public interface IMccService
    {
        Task<IReadOnlyList<MccDto>> GetRecordsAsync(string? filter = null);
        Task<MccDto?> GetByIdAsync(string mccCode);
        Task<MccDto> CreateAsync(CreateMccRequest request, string actingUser);
        Task<MccDto> UpdateAsync(string mccCode, UpdateMccRequest request, string actingUser);
        Task DeleteAsync(string mccCode, string actingUser);
    }
}
