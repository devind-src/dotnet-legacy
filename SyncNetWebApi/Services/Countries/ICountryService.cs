using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Countries;

namespace SyncNetApi.Services.Countries
{
    public interface ICountryService
    {
        Task<IReadOnlyList<CountryDto>> GetRecordsAsync(string? filter = null);
        Task<CountryDto?> GetByIdAsync(string name);
        Task<CountryDto> CreateAsync(CreateCountryRequest request, string actingUser);
        Task<CountryDto> UpdateAsync(string name, UpdateCountryRequest request, string actingUser);
        Task DeleteAsync(string name, string actingUser);
    }
}
