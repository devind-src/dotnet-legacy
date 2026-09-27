using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Currencies;

namespace SyncNetApi.Services.Currencies
{
    public interface ICurrencyService
    {
        Task<IReadOnlyList<CurrencyDto>> GetRecordsAsync(string? filter = null);
        Task<CurrencyDto?> GetByIdAsync(string currencyCode);
        Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, string actingUser);
        Task<CurrencyDto> UpdateAsync(string currencyCode, UpdateCurrencyRequest request, string actingUser);
        Task DeleteAsync(string currencyCode, string actingUser);
    }
}
