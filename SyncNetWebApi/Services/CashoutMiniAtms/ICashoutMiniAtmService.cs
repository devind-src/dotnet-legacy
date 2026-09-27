using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CashoutMiniAtms;

namespace SyncNetApi.Services.CashoutMiniAtms
{
    public interface ICashoutMiniAtmService
    {
        Task<IReadOnlyList<CashoutMiniAtmDto>> GetRecordsAsync(string? filter = null);
        Task<CashoutMiniAtmDto?> GetByIdAsync(long id);
        Task<CashoutMiniAtmDto> CreateAsync(CreateCashoutMiniAtmRequest request, string actingUser);
        Task<CashoutMiniAtmDto> UpdateAsync(long id, UpdateCashoutMiniAtmRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
