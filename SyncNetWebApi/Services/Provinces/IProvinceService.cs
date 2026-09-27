using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Provinces;

namespace SyncNetApi.Services.Provinces
{
    public interface IProvinceService
    {
        Task<IReadOnlyList<ProvinceDto>> GetRecordsAsync(string? filter = null);
        Task<ProvinceDto?> GetByIdAsync(long id);
        Task<ProvinceDto> CreateAsync(CreateProvinceRequest request, string actingUser);
        Task<ProvinceDto> UpdateAsync(long id, UpdateProvinceRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
