using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SyncNetApi.Dtos.JobFees;

namespace SyncNetApi.Services.JobFees
{
    public interface IJobFeeService
    {
        Task<IReadOnlyList<JobFeeDto>> GetRecordsAsync(string? filter = null);
        Task<JobFeeDto?> GetByIdAsync(long id);
        Task<IReadOnlyList<JobFeeDetailDto>> GetDetailsAsync(long id);
        Task<JobFeeDto> CreateAsync(string jobDesc, string? scheduledAt, IFormFile file, string actingUser);
        Task<JobFeePreviewResultDto> PreviewAsync(IFormFile file);
        Task DeleteAsync(long id, string actingUser);
    }
}
