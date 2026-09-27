using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteMargins;
using SyncNetApi.Services.RouteMargins;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/route-margins")]
    public class RouteMarginsController : PermissionGatedControllerBase
    {
        private readonly IRouteMarginService _routes;

        public RouteMarginsController(IRouteMarginService routes, IUserService users) : base(users)
        {
            _routes = routes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteMarginDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _routes.GetRecordsAsync(filter));

        [HttpGet("{instId}")]
        public async Task<ActionResult<RouteMarginDto>> GetById(string instId)
        {
            var item = await _routes.GetByIdAsync(instId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteMarginDto>> Create(CreateRouteMarginRequest request)
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
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{instId}")]
        public async Task<ActionResult<RouteMarginDto>> Update(string instId, UpdateRouteMarginRequest request)
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

        /// <summary>Replaces the routed-product set for one category in a single call — backs
        /// the Routing &gt; Margin checklist form (both "Tambah" and "Edit" call this; the UI
        /// only differs in which checkboxes start disabled).</summary>
        [HttpPut("sync")]
        public async Task<ActionResult<IReadOnlyList<RouteMarginDto>>> Sync(SyncRouteMarginRequest request)
        {
            if (!await CanAsync(p => p.CanAdd || p.CanEdit))
                return Forbidden("You do not have permission to add or edit routes.");

            try
            {
                return Ok(await _routes.SyncCategoryAsync(request, CurrentUser));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

    }
}
