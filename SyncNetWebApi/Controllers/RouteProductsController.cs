using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteProducts;
using SyncNetApi.Services.RouteProducts;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Routing &gt; Product (`/route-product`, table `sw_routes_by_inst`) — a
    /// per-product-code routing rule. NOT to be confused with the "Product" menu/Product
    /// Master (`ProductsController`, `sw_product`).</summary>
    [Route("api/v1/route-products")]
    public class RouteProductsController : PermissionGatedControllerBase
    {
        private readonly IRouteProductService _routes;

        public RouteProductsController(IRouteProductService routes, IUserService users) : base(users)
        {
            _routes = routes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteProductDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _routes.GetRecordsAsync(filter));

        [HttpGet("{instId}")]
        public async Task<ActionResult<RouteProductDto>> GetById(string instId)
        {
            var item = await _routes.GetByIdAsync(instId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteProductDto>> Create(CreateRouteProductRequest request)
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
        public async Task<ActionResult<RouteProductDto>> Update(string instId, UpdateRouteProductRequest request)
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
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
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
