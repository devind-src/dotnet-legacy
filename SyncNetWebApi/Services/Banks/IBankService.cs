using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Banks;

namespace SyncNetApi.Services.Banks
{
    public interface IBankService
    {
        Task<IReadOnlyList<BankDto>> GetRecordsAsync(string? filter = null);
        Task<BankDto?> GetByIdAsync(int id);
        Task<BankDto> CreateAsync(CreateBankRequest request, string actingUser);
        Task<BankDto> UpdateAsync(int id, UpdateBankRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
