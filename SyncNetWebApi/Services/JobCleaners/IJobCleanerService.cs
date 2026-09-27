using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.JobCleaners;

namespace SyncNetApi.Services.JobCleaners
{
    public interface IJobCleanerService
    {
        Task<IReadOnlyList<JobCleanerDto>> GetRecordsAsync(string? filter = null);
        Task<JobCleanerDto?> GetByIdAsync(string entity);
        Task<JobCleanerDto> CreateAsync(CreateJobCleanerRequest request, string actingUser);
        Task<JobCleanerDto> UpdateAsync(string entity, UpdateJobCleanerRequest request, string actingUser);
        Task DeleteAsync(string entity, string actingUser);
    }
}
