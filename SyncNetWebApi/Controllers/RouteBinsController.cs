using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteBins;
using SyncNetApi.Services.RouteBins;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/route-bins")]
    public class RouteBinsController : PermissionGatedControllerBase
    {
        private readonly IRouteBinService _routes;

        public RouteBinsController(IRouteBinService routes, IUserService users) : base(users)
        {
            _routes = routes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteBinDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _routes.GetRecordsAsync(filter));

        [HttpGet("{groupId:int}")]
        public async Task<ActionResult<RouteBinDto>> GetById(int groupId)
        {
            var item = await _routes.GetByIdAsync(groupId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteBinDto>> Create(CreateRouteBinRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add routes.");

            try
            {
                var created = await _routes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { groupId = created.GroupId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Route already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{groupId:int}")]
        public async Task<ActionResult<RouteBinDto>> Update(int groupId, UpdateRouteBinRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit routes.");

            try
            {
                return Ok(await _routes.UpdateAsync(groupId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Route not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{groupId:int}")]
        public async Task<IActionResult> Delete(int groupId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete routes.");

            try
            {
                await _routes.DeleteAsync(groupId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Route not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
