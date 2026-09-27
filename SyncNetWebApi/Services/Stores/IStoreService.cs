using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Stores;

namespace SyncNetApi.Services.Stores
{
    public interface IStoreService
    {
        Task<IReadOnlyList<StoreDto>> GetRecordsAsync(string? filter = null);
        Task<StoreDto?> GetByIdAsync(string storeId);
        Task<StoreDto> CreateAsync(CreateStoreRequest request, string actingUser);
        Task<StoreDto> UpdateAsync(string storeId, UpdateStoreRequest request, string actingUser);
        Task DeleteAsync(string storeId, string actingUser);
    }
}
