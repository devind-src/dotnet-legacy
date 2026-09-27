using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardAccounts;

namespace SyncNetApi.Services.CardAccounts
{
    public interface ICardAccountService
    {
        Task<IReadOnlyList<CardAccountDto>> GetRecordsAsync(string? filter = null);
        Task<CardAccountDto?> GetByIdAsync(int id);
        Task<CardAccountDto> CreateAsync(CreateCardAccountRequest request, string actingUser);
        Task<CardAccountDto> UpdateAsync(int id, UpdateCardAccountRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
