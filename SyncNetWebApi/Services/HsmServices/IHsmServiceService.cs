using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.HsmServices;

namespace SyncNetApi.Services.HsmServices
{
    public interface IHsmServiceService
    {
        Task<IReadOnlyList<HsmServiceDto>> GetRecordsAsync(string? filter = null);
        Task<HsmServiceDto?> GetByIdAsync(string hsmDesc);
        Task<HsmServiceDto> CreateAsync(CreateHsmServiceRequest request, string actingUser);
        Task<HsmServiceDto> UpdateAsync(string hsmDesc, UpdateHsmServiceRequest request, string actingUser);
        Task DeleteAsync(string hsmDesc, string actingUser);
    }
}
