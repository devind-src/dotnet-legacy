using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductFeeAdditionals;
using SyncNetApi.Services.ProductFeeAdditionals;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product-fee-additionals")]
    public class ProductFeeAdditionalsController : PermissionGatedControllerBase
    {
        private readonly IProductFeeAdditionalService _fees;

        public ProductFeeAdditionalsController(IProductFeeAdditionalService fees, IUserService users) : base(users)
        {
            _fees = fees;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductFeeAdditionalDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _fees.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductFeeAdditionalDto>> GetById(long id)
        {
            var item = await _fees.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductFeeAdditionalDto>> Create(CreateProductFeeAdditionalRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add additional fees.");

            var created = await _fees.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductFeeAdditionalDto>> Update(long id, UpdateProductFeeAdditionalRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit additional fees.");

            try
            {
                return Ok(await _fees.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Additional fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete additional fees.");

            try
            {
                await _fees.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Additional fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
