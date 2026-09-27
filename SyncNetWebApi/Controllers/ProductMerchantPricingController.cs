using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductMerchantPricing;
using SyncNetApi.Services.ProductMerchantPricing;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/pricing-fees/merchant-pricing")]
    public class ProductMerchantPricingController : PermissionGatedControllerBase
    {
        private readonly IProductMerchantPricingService _prices;

        public ProductMerchantPricingController(IProductMerchantPricingService prices, IUserService users) : base(users)
        {
            _prices = prices;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductMerchantPricingDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _prices.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductMerchantPricingDto>> GetById(int id)
        {
            var item = await _prices.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductMerchantPricingDto>> Create(CreateProductMerchantPricingRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add merchant prices.");

            try
            {
                var created = await _prices.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductMerchantPricingDto>> Update(int id, UpdateProductMerchantPricingRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit merchant prices.");

            try
            {
                return Ok(await _prices.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant price not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete merchant prices.");

            try
            {
                await _prices.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant price not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
