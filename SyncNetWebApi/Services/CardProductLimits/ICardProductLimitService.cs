using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardProductLimits;

namespace SyncNetApi.Services.CardProductLimits
{
    public interface ICardProductLimitService
    {
        Task<IReadOnlyList<CardProductLimitDto>> GetByProductIdAsync(int productId);
        Task<CardProductLimitDto?> GetByIdAsync(int id);
        Task<CardProductLimitDto> CreateAsync(CreateCardProductLimitRequest request, string actingUser);
        Task<CardProductLimitDto> UpdateAsync(int id, UpdateCardProductLimitRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
