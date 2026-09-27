using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Participants;

namespace SyncNetApi.Services.Participants
{
    public interface IParticipantService
    {
        Task<IReadOnlyList<ParticipantDto>> GetRecordsAsync(string? filter = null);
        Task<ParticipantDto?> GetByIdAsync(string participantId);
        Task<ParticipantDto> CreateAsync(CreateParticipantRequest request, string actingUser);
        Task<ParticipantDto> UpdateAsync(string participantId, UpdateParticipantRequest request, string actingUser);
        Task DeleteAsync(string participantId, string actingUser);
    }
}
