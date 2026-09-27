using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.PublicHolidays;

namespace SyncNetApi.Services.PublicHolidays
{
    public interface IPublicHolidayService
    {
        Task<IReadOnlyList<PublicHolidayDto>> GetRecordsAsync(string? filter = null);
        Task<PublicHolidayDto?> GetByIdAsync(string holidayDate);
        Task<PublicHolidayDto> CreateAsync(CreatePublicHolidayRequest request, string actingUser);
        Task<PublicHolidayDto> UpdateAsync(string holidayDate, UpdatePublicHolidayRequest request, string actingUser);
        Task DeleteAsync(string holidayDate, string actingUser);
    }
}
