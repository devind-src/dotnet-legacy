using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.AccountTypes;

namespace SyncNetApi.Services.AccountTypes
{
    public interface IAccountTypeService
    {
        Task<IReadOnlyList<AccountTypeDto>> GetRecordsAsync(string? filter = null);
        Task<AccountTypeDto?> GetByIdAsync(string acctType);
        Task<AccountTypeDto> CreateAsync(CreateAccountTypeRequest request, string actingUser);
        Task<AccountTypeDto> UpdateAsync(string acctType, UpdateAccountTypeRequest request, string actingUser);
        Task DeleteAsync(string acctType, string actingUser);
    }
}
