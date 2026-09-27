using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductMerchants;
using SyncNetApi.Services.ProductMerchants;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/master/merchants")]
    public class ProductMerchantsController : PermissionGatedControllerBase
    {
        private readonly IProductMerchantService _merchants;

        public ProductMerchantsController(IProductMerchantService merchants, IUserService users) : base(users)
        {
            _merchants = merchants;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductMerchantDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _merchants.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductMerchantDto>> GetById(int id)
        {
            var item = await _merchants.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductMerchantDto>> Create(CreateProductMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product merchants.");

            try
            {
                var created = await _merchants.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductMerchantDto>> Update(int id, UpdateProductMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product merchants.");

            try
            {
                return Ok(await _merchants.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
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
                return Forbidden("You do not have permission to delete product merchants.");

            try
            {
                await _merchants.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
