using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Connections;
using SyncNetApi.Services.Connections;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/connections")]
    public class ConnectionsController : PermissionGatedControllerBase
    {
        private readonly IConnectionService _connections;

        public ConnectionsController(IConnectionService connections, IUserService users) : base(users)
        {
            _connections = connections;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ConnectionDto>>> GetByNode([FromQuery] int nodeId)
            => Ok(await _connections.GetByNodeIdAsync(nodeId));

        [HttpGet("{connName}")]
        public async Task<ActionResult<ConnectionDto>> GetById(string connName)
        {
            var item = await _connections.GetByIdAsync(connName);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ConnectionDto>> Create(CreateConnectionRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add connections.");

            try
            {
                var created = await _connections.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { connName = created.ConnName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Connection already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{connName}")]
        public async Task<ActionResult<ConnectionDto>> Update(string connName, UpdateConnectionRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit connections.");

            try
            {
                return Ok(await _connections.UpdateAsync(connName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Connection not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{connName}")]
        public async Task<IActionResult> Delete(string connName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete connections.");

            try
            {
                await _connections.DeleteAsync(connName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Connection not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
