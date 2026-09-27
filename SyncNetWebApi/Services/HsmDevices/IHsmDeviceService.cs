using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.HsmDevices;

namespace SyncNetApi.Services.HsmDevices
{
    public interface IHsmDeviceService
    {
        Task<IReadOnlyList<HsmDeviceDto>> GetRecordsAsync(string? filter = null);
        Task<HsmDeviceDto?> GetByIdAsync(string hsmName);
        Task<HsmDeviceDto> CreateAsync(CreateHsmDeviceRequest request, string actingUser);
        Task<HsmDeviceDto> UpdateAsync(string hsmName, UpdateHsmDeviceRequest request, string actingUser);
        Task DeleteAsync(string hsmName, string actingUser);
    }
}
