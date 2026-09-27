using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Brands;

namespace SyncNetApi.Services.Brands
{
    public interface IBrandService
    {
        Task<IReadOnlyList<BrandDto>> GetRecordsAsync(string? filter = null);
        Task<BrandDto?> GetByIdAsync(int id);
        Task<BrandDto> CreateAsync(CreateBrandRequest request, string actingUser);
        Task<BrandDto> UpdateAsync(int id, UpdateBrandRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
