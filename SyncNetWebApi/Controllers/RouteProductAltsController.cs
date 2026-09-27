using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteProductAlts;
using SyncNetApi.Services.RouteProductAlts;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Panel detail "Alternate Biller" di Routing &gt; Product (`/route-product`):
    /// biller cadangan per produk bill payment, model primary-backup (bukan load balancer).</summary>
    [Route("api/v1/route-product-alts")]
    public class RouteProductAltsController : PermissionGatedControllerBase
    {
        private readonly IRouteProductAltService _alts;

        public RouteProductAltsController(IRouteProductAltService alts, IUserService users) : base(users)
        {
            _alts = alts;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteProductAltDto>>> GetByProduct([FromQuery] string instId)
            => Ok(await _alts.GetByProductAsync(instId));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RouteProductAltDto>> GetById(int id)
        {
            var item = await _alts.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteProductAltDto>> Create(CreateRouteProductAltRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add alternate billers.");

            try
            {
                var created = await _alts.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Alternate already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<RouteProductAltDto>> Update(int id, UpdateRouteProductAltRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit alternate billers.");

            try
            {
                return Ok(await _alts.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Alternate not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Alternate conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
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
                return Forbidden("You do not have permission to delete alternate billers.");

            try
            {
                await _alts.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Alternate not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
