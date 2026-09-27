using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Cities;

namespace SyncNetApi.Services.Cities
{
    public interface ICityService
    {
        Task<IReadOnlyList<CityDto>> GetRecordsAsync(string? filter = null);
        Task<CityDto?> GetByIdAsync(long id);
        Task<CityDto> CreateAsync(CreateCityRequest request, string actingUser);
        Task<CityDto> UpdateAsync(long id, UpdateCityRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
