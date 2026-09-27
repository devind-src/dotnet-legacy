using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteSources;
using SyncNetApi.Services.RouteSources;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/route-sources")]
    public class RouteSourcesController : PermissionGatedControllerBase
    {
        private readonly IRouteSourceService _routes;

        public RouteSourcesController(IRouteSourceService routes, IUserService users) : base(users)
        {
            _routes = routes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteSourceDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _routes.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RouteSourceDto>> GetById(int id)
        {
            var item = await _routes.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteSourceDto>> Create(CreateRouteSourceRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add routes.");

            try
            {
                var created = await _routes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<RouteSourceDto>> Update(int id, UpdateRouteSourceRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit routes.");

            try
            {
                return Ok(await _routes.UpdateAsync(id, request, CurrentUser));
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete routes.");

            try
            {
                await _routes.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Route not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
