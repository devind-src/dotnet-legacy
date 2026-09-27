using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.VaAccounts;

namespace SyncNetApi.Services.VaAccounts
{
    public interface IVaAccountService
    {
        Task<IReadOnlyList<VaAccountDto>> GetRecordsAsync(string? filter = null);
        Task<VaAccountDto?> GetByIdAsync(string accNr);
        Task<VaAccountDto> CreateAsync(CreateVaAccountRequest request, string actingUser);
        Task<VaAccountDto> UpdateAsync(string accNr, UpdateVaAccountRequest request, string actingUser);
        Task DeleteAsync(string accNr, string actingUser);
    }
}
