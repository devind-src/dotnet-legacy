using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteDynamics;
using SyncNetApi.Services.RouteDynamics;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/route-dynamics")]
    public class RouteDynamicsController : PermissionGatedControllerBase
    {
        private readonly IRouteDynamicService _routes;

        public RouteDynamicsController(IRouteDynamicService routes, IUserService users) : base(users)
        {
            _routes = routes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteDynamicDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _routes.GetRecordsAsync(filter));

        [HttpGet("{instId}")]
        public async Task<ActionResult<RouteDynamicDto>> GetById(string instId)
        {
            var item = await _routes.GetByIdAsync(instId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteDynamicDto>> Create(CreateRouteDynamicRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add routes.");

            try
            {
                var created = await _routes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { instId = created.InstId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Route already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{instId}")]
        public async Task<ActionResult<RouteDynamicDto>> Update(string instId, UpdateRouteDynamicRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit routes.");

            try
            {
                return Ok(await _routes.UpdateAsync(instId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Route not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{instId}")]
        public async Task<IActionResult> Delete(string instId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete routes.");

            try
            {
                await _routes.DeleteAsync(instId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Route not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
