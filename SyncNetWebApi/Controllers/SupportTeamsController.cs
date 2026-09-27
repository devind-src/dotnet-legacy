using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.SupportTeams;
using SyncNetApi.Services.SupportTeams;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/support-teams")]
    public class SupportTeamsController : PermissionGatedControllerBase
    {
        private readonly ISupportTeamService _teams;

        public SupportTeamsController(ISupportTeamService teams, IUserService users) : base(users)
        {
            _teams = teams;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SupportTeamDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _teams.GetRecordsAsync(filter));

        [HttpGet("{teamId:int}")]
        public async Task<ActionResult<SupportTeamDto>> GetById(int teamId)
        {
            var item = await _teams.GetByIdAsync(teamId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<SupportTeamDto>> Create(CreateSupportTeamRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add support teams.");

            var created = await _teams.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { teamId = created.TeamId }, created);
        }

        [HttpPut("{teamId:int}")]
        public async Task<ActionResult<SupportTeamDto>> Update(int teamId, UpdateSupportTeamRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit support teams.");

            try
            {
                return Ok(await _teams.UpdateAsync(teamId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Support team not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{teamId:int}")]
        public async Task<IActionResult> Delete(int teamId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete support teams.");

            try
            {
                await _teams.DeleteAsync(teamId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Support team not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Support team in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
