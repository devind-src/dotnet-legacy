using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.JobFees;
using SyncNetApi.Services.JobFees;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/job-fees")]
    public class JobFeesController : PermissionGatedControllerBase
    {
        private readonly IJobFeeService _jobFees;

        public JobFeesController(IJobFeeService jobFees, IUserService users) : base(users)
        {
            _jobFees = jobFees;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<JobFeeDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _jobFees.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<JobFeeDto>> GetById(long id)
        {
            var item = await _jobFees.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("{id:long}/details")]
        public async Task<ActionResult<IReadOnlyList<JobFeeDetailDto>>> GetDetails(long id)
        {
            var item = await _jobFees.GetByIdAsync(id);
            if (item == null) return NotFound();

            return Ok(await _jobFees.GetDetailsAsync(id));
        }

        /// <summary>Preview-only: parses and validates the CSV the same way Create does, but
        /// never persists anything — lets the WASM upload dialog show a valid/invalid preview
        /// table before the user commits to Simpan.</summary>
        [HttpPost("preview")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<JobFeePreviewResultDto>> Preview(IFormFile file)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add job fees.");

            try
            {
                return Ok(await _jobFees.PreviewAsync(file));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        /// <summary>Legacy has no Update path for Job Fee — master is read-only after create
        /// (view + delete only). This API deliberately exposes no PUT endpoint.</summary>
        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<JobFeeDto>> Create([FromForm] string jobDesc, [FromForm] string? scheduledAt, IFormFile file)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add job fees.");

            try
            {
                var created = await _jobFees.CreateAsync(jobDesc, scheduledAt, file, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete job fees.");

            try
            {
                await _jobFees.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Job fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
