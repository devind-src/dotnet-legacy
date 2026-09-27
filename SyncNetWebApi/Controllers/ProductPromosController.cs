using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductPromos;
using SyncNetApi.Services.ProductPromos;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product-promos")]
    public class ProductPromosController : PermissionGatedControllerBase
    {
        private readonly IProductPromoService _promos;

        public ProductPromosController(IProductPromoService promos, IUserService users) : base(users)
        {
            _promos = promos;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductPromoDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _promos.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductPromoDto>> GetById(long id)
        {
            var item = await _promos.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductPromoDto>> Create(CreateProductPromoRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product promos.");

            try
            {
                var created = await _promos.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Product promo overlaps", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductPromoDto>> Update(long id, UpdateProductPromoRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product promos.");

            try
            {
                return Ok(await _promos.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product promo not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Product promo overlaps", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product promos.");

            try
            {
                await _promos.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product promo not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
