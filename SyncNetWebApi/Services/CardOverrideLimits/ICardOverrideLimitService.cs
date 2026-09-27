using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardOverrideLimits;

namespace SyncNetApi.Services.CardOverrideLimits
{
    public interface ICardOverrideLimitService
    {
        Task<IReadOnlyList<CardOverrideLimitDto>> GetRecordsAsync(string? filter = null);
        Task<CardOverrideLimitDto?> GetByIdAsync(int id);
        Task<CardOverrideLimitDto> CreateAsync(CreateCardOverrideLimitRequest request, string actingUser);
        Task<CardOverrideLimitDto> UpdateAsync(int id, UpdateCardOverrideLimitRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
