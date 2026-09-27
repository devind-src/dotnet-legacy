using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Nodes;
using SyncNetApi.Services.Nodes;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/nodes")]
    public class NodesController : PermissionGatedControllerBase
    {
        private readonly INodeService _nodes;

        public NodesController(INodeService nodes, IUserService users) : base(users)
        {
            _nodes = nodes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<NodeDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _nodes.GetRecordsAsync(filter));

        [HttpGet("new-ports")]
        public async Task<ActionResult> GetNewPorts()
        {
            var (portIn, portOut) = await _nodes.GetNewPortsAsync();
            return Ok(new { portIn, portOut });
        }

        [HttpGet("{nodeId:int}")]
        public async Task<ActionResult<NodeDto>> GetById(int nodeId)
        {
            var item = await _nodes.GetByIdAsync(nodeId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<NodeDto>> Create(CreateNodeRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add nodes.");

            try
            {
                var created = await _nodes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { nodeId = created.NodeId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Node already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{nodeId:int}")]
        public async Task<ActionResult<NodeDto>> Update(int nodeId, UpdateNodeRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit nodes.");

            try
            {
                return Ok(await _nodes.UpdateAsync(nodeId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Node not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{nodeId:int}")]
        public async Task<IActionResult> Delete(int nodeId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete nodes.");

            try
            {
                await _nodes.DeleteAsync(nodeId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Node not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Node masih dipakai", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
