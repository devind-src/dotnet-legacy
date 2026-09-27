using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.AgentBanks;
using SyncNetApi.Services.AgentBanks;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/agent-banks")]
    public class AgentBanksController : PermissionGatedControllerBase
    {
        private readonly IAgentBankService _agentBanks;

        public AgentBanksController(IAgentBankService agentBanks, IUserService users) : base(users)
        {
            _agentBanks = agentBanks;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AgentBankDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _agentBanks.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AgentBankDto>> GetById(long id)
        {
            var item = await _agentBanks.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<AgentBankDto>> Create(CreateAgentBankRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add QRIS static agent bank accounts.");

            try
            {
                var created = await _agentBanks.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<AgentBankDto>> Update(long id, UpdateAgentBankRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit QRIS static agent bank accounts.");

            try
            {
                return Ok(await _agentBanks.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Agent bank not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete QRIS static agent bank accounts.");

            try
            {
                await _agentBanks.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Agent bank not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
