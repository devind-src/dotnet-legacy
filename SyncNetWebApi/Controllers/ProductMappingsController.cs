using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductMappings;
using SyncNetApi.Services.ProductMappings;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/master/mappings")]
    public class ProductMappingsController : PermissionGatedControllerBase
    {
        private readonly IProductMappingService _mappings;

        public ProductMappingsController(IProductMappingService mappings, IUserService users) : base(users)
        {
            _mappings = mappings;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductMappingDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _mappings.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductMappingDto>> GetById(long id)
        {
            var item = await _mappings.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductMappingDto>> Create(CreateProductMappingRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product mappings.");

            var created = await _mappings.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductMappingDto>> Update(long id, UpdateProductMappingRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product mappings.");

            try
            {
                return Ok(await _mappings.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product mapping not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product mappings.");

            try
            {
                await _mappings.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product mapping not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
