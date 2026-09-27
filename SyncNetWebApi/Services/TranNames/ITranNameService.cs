using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.TranNames;

namespace SyncNetApi.Services.TranNames
{
    public interface ITranNameService
    {
        Task<IReadOnlyList<TranNameDto>> GetRecordsAsync(string? filter = null);
        Task<TranNameDto?> GetByIdAsync(string transCode);
        Task<TranNameDto> CreateAsync(CreateTranNameRequest request, string actingUser);
        Task<TranNameDto> UpdateAsync(string transCode, UpdateTranNameRequest request, string actingUser);
        Task DeleteAsync(string transCode, string actingUser);
    }
}
