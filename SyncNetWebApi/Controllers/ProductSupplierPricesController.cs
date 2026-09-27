using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductSupplierPrices;
using SyncNetApi.Services.ProductSupplierPrices;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product-supplier-prices")]
    public class ProductSupplierPricesController : PermissionGatedControllerBase
    {
        private readonly IProductSupplierPriceService _prices;

        public ProductSupplierPricesController(IProductSupplierPriceService prices, IUserService users) : base(users)
        {
            _prices = prices;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductSupplierPriceDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _prices.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductSupplierPriceDto>> GetById(int id)
        {
            var item = await _prices.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductSupplierPriceDto>> Create(CreateProductSupplierPriceRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add supplier prices.");

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
        public async Task<ActionResult<ProductSupplierPriceDto>> Update(int id, UpdateProductSupplierPriceRequest request)
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
