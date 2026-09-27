using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.SoundBoxes;

namespace SyncNetApi.Services.SoundBoxes
{
    public interface ISoundBoxService
    {
        Task<IReadOnlyList<SoundBoxDto>> GetRecordsAsync(string? filter = null);
        Task<SoundBoxDto?> GetByIdAsync(string nmid);
        Task<SoundBoxDto> CreateAsync(CreateSoundBoxRequest request, string actingUser);
        Task<SoundBoxDto> UpdateAsync(string nmid, UpdateSoundBoxRequest request, string actingUser);
        Task DeleteAsync(string nmid, string actingUser);
    }
}
