using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductPrepaidPricing;
using SyncNetApi.Services.ProductPrepaidPricing;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/pricing-fees/prepaid-pricing")]
    public class ProductPrepaidPricingController : PermissionGatedControllerBase
    {
        private readonly IProductPrepaidPricingService _prices;

        public ProductPrepaidPricingController(IProductPrepaidPricingService prices, IUserService users) : base(users)
        {
            _prices = prices;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductPrepaidPricingDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _prices.GetRecordsAsync(filter));

        [HttpGet("products")]
        public async Task<ActionResult<IReadOnlyList<TopupRoutingDto>>> GetProducts([FromQuery] string? filter)
            => Ok(await _prices.GetTopupRoutingAsync(filter));

        [HttpPut("products/{productId}/routing")]
        public async Task<ActionResult<TopupRoutingDto>> UpdateRouting(string productId, UpdateTopupRoutingRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit supplier prices.");

            try
            {
                return Ok(await _prices.UpdateTopupRoutingAsync(productId, request, CurrentUser));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("products/{productId}/weights")]
        public async Task<ActionResult<IReadOnlyList<ProductPrepaidPricingDto>>> UpdateWeights(string productId, UpdateSupplierWeightsRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit supplier prices.");

            try
            {
                return Ok(await _prices.UpdateWeightsAsync(productId, request, CurrentUser));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("{id:int}/apply-all-denoms")]
        public async Task<ActionResult<IReadOnlyList<ProductPrepaidPricingDto>>> ApplyToAllDenoms(int id)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit supplier prices.");

            try
            {
                return Ok(await _prices.ApplyToAllDenomsAsync(id, CurrentUser));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Supplier price not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductPrepaidPricingDto>> GetById(int id)
        {
            var item = await _prices.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductPrepaidPricingDto>> Create(CreateProductPrepaidPricingRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add supplier prices.");

            try
            {
                var created = await _prices.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductPrepaidPricingDto>> Update(int id, UpdateProductPrepaidPricingRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit supplier prices.");

            try
            {
                return Ok(await _prices.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Supplier price not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
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
                return Forbidden("You do not have permission to delete supplier prices.");

            try
            {
                await _prices.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Supplier price not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
