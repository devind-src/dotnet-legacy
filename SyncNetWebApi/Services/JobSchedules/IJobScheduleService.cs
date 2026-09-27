using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.JobSchedules;

namespace SyncNetApi.Services.JobSchedules
{
    public interface IJobScheduleService
    {
        Task<IReadOnlyList<JobScheduleDto>> GetRecordsAsync(string? filter = null);
        Task<JobScheduleDto?> GetByIdAsync(int jobId);
        Task<JobScheduleDto> CreateAsync(CreateJobScheduleRequest request, string actingUser);
        Task<JobScheduleDto> UpdateAsync(int jobId, UpdateJobScheduleRequest request, string actingUser);
        Task DeleteAsync(int jobId, string actingUser);
    }
}
