using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductAdditionalFees;
using SyncNetApi.Services.ProductAdditionalFees;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/pricing-fees/additional-fees")]
    public class ProductAdditionalFeesController : PermissionGatedControllerBase
    {
        private readonly IProductAdditionalFeeService _fees;

        public ProductAdditionalFeesController(IProductAdditionalFeeService fees, IUserService users) : base(users)
        {
            _fees = fees;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductAdditionalFeeDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _fees.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductAdditionalFeeDto>> GetById(long id)
        {
            var item = await _fees.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductAdditionalFeeDto>> Create(CreateProductAdditionalFeeRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add additional fees.");

            var created = await _fees.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductAdditionalFeeDto>> Update(long id, UpdateProductAdditionalFeeRequest request)
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
