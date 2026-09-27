using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardGroups;

namespace SyncNetApi.Services.CardGroups
{
    public interface ICardGroupService
    {
        Task<IReadOnlyList<CardGroupDto>> GetRecordsAsync(string? filter = null);
        Task<CardGroupDto?> GetByIdAsync(int groupId);
        Task<CardGroupDto> CreateAsync(CreateCardGroupRequest request, string actingUser);
        Task<CardGroupDto> UpdateAsync(int groupId, UpdateCardGroupRequest request, string actingUser);
        Task DeleteAsync(int groupId, string actingUser);
    }
}
