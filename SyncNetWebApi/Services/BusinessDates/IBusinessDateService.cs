using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.BusinessDates;

namespace SyncNetApi.Services.BusinessDates
{
    public interface IBusinessDateService
    {
        Task<IReadOnlyList<BusinessDateDto>> GetRecordsAsync(string? filter = null);
        Task<BusinessDateDto?> GetByIdAsync(string businessCalendar);
        Task<BusinessDateDto> CreateAsync(CreateBusinessDateRequest request, string actingUser);
        Task<BusinessDateDto> UpdateAsync(string businessCalendar, UpdateBusinessDateRequest request, string actingUser);
        Task DeleteAsync(string businessCalendar, string actingUser);
    }
}
