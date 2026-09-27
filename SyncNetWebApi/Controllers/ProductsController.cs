using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Products;
using SyncNetApi.Services.Products;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/master/products")]
    public class ProductsController : PermissionGatedControllerBase
    {
        private readonly IProductService _products;

        public ProductsController(IProductService products, IUserService users) : base(users)
        {
            _products = products;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _products.GetRecordsAsync(filter));

        [HttpGet("{productCode}")]
        public async Task<ActionResult<ProductDto>> GetById(string productCode)
        {
            var item = await _products.GetByIdAsync(productCode);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add products.");

            try
            {
                var created = await _products.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { productCode = created.ProductCode }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Product already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{productCode}")]
        public async Task<ActionResult<ProductDto>> Update(string productCode, UpdateProductRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit products.");

            try
            {
                return Ok(await _products.UpdateAsync(productCode, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{productCode}")]
        public async Task<IActionResult> Delete(string productCode)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete products.");

            try
            {
                await _products.DeleteAsync(productCode, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
