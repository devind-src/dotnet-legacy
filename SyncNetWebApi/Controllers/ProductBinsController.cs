using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductBins;
using SyncNetApi.Services.ProductBins;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product-bins")]
    public class ProductBinsController : PermissionGatedControllerBase
    {
        private readonly IProductBinService _productBins;

        public ProductBinsController(IProductBinService productBins, IUserService users) : base(users)
        {
            _productBins = productBins;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductBinDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _productBins.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductBinDto>> GetById(long id)
        {
            var item = await _productBins.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductBinDto>> Create(CreateProductBinRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add BINs.");

            var created = await _productBins.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductBinDto>> Update(long id, UpdateProductBinRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit BINs.");

            try
            {
                return Ok(await _productBins.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete BINs.");

            try
            {
                await _productBins.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
