using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SyncNetApi.Dtos.TerminalImages;

namespace SyncNetApi.Services.TerminalImages
{
    public interface ITerminalImageService
    {
        Task<IReadOnlyList<TerminalImageDto>> GetRecordsAsync(string? filter = null);
        Task<TerminalImageDto?> GetByNameAsync(string name);
        Task<TerminalImageDto> CreateAsync(CreateTerminalImageRequest request, string actingUser);
        Task<TerminalImageDto> UpdateAsync(string name, UpdateTerminalImageRequest request, string actingUser);
        Task DeleteAsync(string name, string actingUser);

        /// <summary>slot is 1-5, matching banner_1..banner_5 (the only slots the legacy UI
        /// exposes — see SwTerminalImage entity note for why "logo" is excluded).</summary>
        Task<TerminalImageDto> UploadBannerAsync(string name, int slot, IFormFile file, string actingUser);
    }
}
