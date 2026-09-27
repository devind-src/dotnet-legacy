using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Participants;
using SyncNetApi.Services.Participants;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/participants")]
    public class ParticipantsController : PermissionGatedControllerBase
    {
        private readonly IParticipantService _participants;

        public ParticipantsController(IParticipantService participants, IUserService users) : base(users)
        {
            _participants = participants;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ParticipantDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _participants.GetRecordsAsync(filter));

        [HttpGet("{participantId}")]
        public async Task<ActionResult<ParticipantDto>> GetById(string participantId)
        {
            var participant = await _participants.GetByIdAsync(participantId);
            return participant == null ? NotFound() : Ok(participant);
        }

        [HttpPost]
        public async Task<ActionResult<ParticipantDto>> Create(CreateParticipantRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add participants.");

            try
            {
                var created = await _participants.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { participantId = created.ParticipantId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Participant already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{participantId}")]
        public async Task<ActionResult<ParticipantDto>> Update(string participantId, UpdateParticipantRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit participants.");

            try
            {
                return Ok(await _participants.UpdateAsync(participantId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Participant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{participantId}")]
        public async Task<IActionResult> Delete(string participantId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete participants.");

            try
            {
                await _participants.DeleteAsync(participantId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Participant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Participant in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
