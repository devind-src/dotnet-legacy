using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Terminals;
using SyncNetApi.Services.Terminals;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/terminals")]
    public class TerminalsController : PermissionGatedControllerBase
    {
        private readonly ITerminalService _terminals;

        public TerminalsController(ITerminalService terminals, IUserService users) : base(users)
        {
            _terminals = terminals;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TerminalDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _terminals.GetRecordsAsync(filter));

        [HttpGet("{termId}")]
        public async Task<ActionResult<TerminalDto>> GetById(string termId)
        {
            var terminal = await _terminals.GetByIdAsync(termId);
            return terminal == null ? NotFound() : Ok(terminal);
        }

        [HttpPost]
        public async Task<ActionResult<TerminalDto>> Create(CreateTerminalRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add terminals.");

            try
            {
                var created = await _terminals.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { termId = created.TermId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Terminal already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{termId}")]
        public async Task<ActionResult<TerminalDto>> Update(string termId, UpdateTerminalRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit terminals.");

            try
            {
                return Ok(await _terminals.UpdateAsync(termId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Terminal not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Terminal conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpDelete("{termId}")]
        public async Task<IActionResult> Delete(string termId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete terminals.");

            try
            {
                await _terminals.DeleteAsync(termId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Terminal not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
