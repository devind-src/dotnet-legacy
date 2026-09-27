using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.TerminalLimits;
using SyncNetApi.Services.TerminalLimits;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/terminal-limits")]
    public class TerminalLimitsController : PermissionGatedControllerBase
    {
        private readonly ITerminalLimitService _limits;

        public TerminalLimitsController(ITerminalLimitService limits, IUserService users) : base(users)
        {
            _limits = limits;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TerminalLimitDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _limits.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<TerminalLimitDto>> GetById(long id)
        {
            var limit = await _limits.GetByIdAsync(id);
            return limit == null ? NotFound() : Ok(limit);
        }

        [HttpPost]
        public async Task<ActionResult<TerminalLimitDto>> Create(CreateTerminalLimitRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add limits.");

            try
            {
                var created = await _limits.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Limit already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<TerminalLimitDto>> Update(long id, UpdateTerminalLimitRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit limits.");

            try
            {
                return Ok(await _limits.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete limits.");

            try
            {
                await _limits.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
