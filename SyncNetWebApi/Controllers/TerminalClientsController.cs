using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.TerminalClients;
using SyncNetApi.Services.TerminalClients;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/terminal-clients")]
    public class TerminalClientsController : PermissionGatedControllerBase
    {
        private readonly ITerminalClientService _clients;

        public TerminalClientsController(ITerminalClientService clients, IUserService users) : base(users)
        {
            _clients = clients;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TerminalClientDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _clients.GetRecordsAsync(filter));

        [HttpGet("{clientId}")]
        public async Task<ActionResult<TerminalClientDto>> GetById(string clientId)
        {
            var client = await _clients.GetByIdAsync(clientId);
            return client == null ? NotFound() : Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<TerminalClientDto>> Create(CreateTerminalClientRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add credentials.");

            try
            {
                var created = await _clients.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { clientId = created.ClientId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Credential already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{clientId}")]
        public async Task<ActionResult<TerminalClientDto>> Update(string clientId, UpdateTerminalClientRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit credentials.");

            try
            {
                return Ok(await _clients.UpdateAsync(clientId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Credential not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{clientId}")]
        public async Task<IActionResult> Delete(string clientId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete credentials.");

            try
            {
                await _clients.DeleteAsync(clientId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Credential not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
