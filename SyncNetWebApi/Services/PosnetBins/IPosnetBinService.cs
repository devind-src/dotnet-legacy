using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.PosnetBins;

namespace SyncNetApi.Services.PosnetBins
{
    public interface IPosnetBinService
    {
        Task<IReadOnlyList<PosnetBinDto>> GetRecordsAsync(string? filter = null);
        Task<PosnetBinDto?> GetByIdAsync(int id);
        Task<PosnetBinDto> CreateAsync(CreatePosnetBinRequest request, string actingUser);
        Task<PosnetBinDto> UpdateAsync(int id, UpdatePosnetBinRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
