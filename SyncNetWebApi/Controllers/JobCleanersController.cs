using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.JobCleaners;
using SyncNetApi.Services.JobCleaners;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/job-cleaners")]
    public class JobCleanersController : PermissionGatedControllerBase
    {
        private readonly IJobCleanerService _cleaners;

        public JobCleanersController(IJobCleanerService cleaners, IUserService users) : base(users)
        {
            _cleaners = cleaners;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<JobCleanerDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cleaners.GetRecordsAsync(filter));

        [HttpGet("{entity}")]
        public async Task<ActionResult<JobCleanerDto>> GetById(string entity)
        {
            var item = await _cleaners.GetByIdAsync(entity);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<JobCleanerDto>> Create(CreateJobCleanerRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add job cleaners.");

            try
            {
                var created = await _cleaners.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { entity = created.Entity }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Job cleaner already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{entity}")]
        public async Task<ActionResult<JobCleanerDto>> Update(string entity, UpdateJobCleanerRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit job cleaners.");

            try
            {
                return Ok(await _cleaners.UpdateAsync(entity, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Job cleaner not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{entity}")]
        public async Task<IActionResult> Delete(string entity)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete job cleaners.");

            try
            {
                await _cleaners.DeleteAsync(entity, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Job cleaner not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
