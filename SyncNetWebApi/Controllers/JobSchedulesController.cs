using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.JobSchedules;
using SyncNetApi.Services.JobSchedules;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/job-schedules")]
    public class JobSchedulesController : PermissionGatedControllerBase
    {
        private readonly IJobScheduleService _jobs;

        public JobSchedulesController(IJobScheduleService jobs, IUserService users) : base(users)
        {
            _jobs = jobs;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<JobScheduleDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _jobs.GetRecordsAsync(filter));

        [HttpGet("{jobId:int}")]
        public async Task<ActionResult<JobScheduleDto>> GetById(int jobId)
        {
            var item = await _jobs.GetByIdAsync(jobId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<JobScheduleDto>> Create(CreateJobScheduleRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add job schedules.");

            var created = await _jobs.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { jobId = created.JobId }, created);
        }

        [HttpPut("{jobId:int}")]
        public async Task<ActionResult<JobScheduleDto>> Update(int jobId, UpdateJobScheduleRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit job schedules.");

            try
            {
                return Ok(await _jobs.UpdateAsync(jobId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Job not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{jobId:int}")]
        public async Task<IActionResult> Delete(int jobId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete job schedules.");

            try
            {
                await _jobs.DeleteAsync(jobId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Job not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
