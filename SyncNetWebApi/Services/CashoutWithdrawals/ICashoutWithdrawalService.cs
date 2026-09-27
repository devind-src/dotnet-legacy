using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CashoutWithdrawals;

namespace SyncNetApi.Services.CashoutWithdrawals
{
    public interface ICashoutWithdrawalService
    {
        Task<IReadOnlyList<CashoutWithdrawalDto>> GetRecordsAsync(string? filter = null);
        Task<CashoutWithdrawalDto?> GetByIdAsync(long id);
        Task<CashoutWithdrawalDto> CreateAsync(CreateCashoutWithdrawalRequest request, string actingUser);
        Task<CashoutWithdrawalDto> UpdateAsync(long id, UpdateCashoutWithdrawalRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
