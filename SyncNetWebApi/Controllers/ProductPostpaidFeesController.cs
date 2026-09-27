using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductPostpaidFees;
using SyncNetApi.Services.ProductPostpaidFees;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/pricing-fees/postpaid-fees")]
    public class ProductPostpaidFeesController : PermissionGatedControllerBase
    {
        private readonly IProductPostpaidFeeService _fees;

        public ProductPostpaidFeesController(IProductPostpaidFeeService fees, IUserService users) : base(users)
        {
            _fees = fees;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductPostpaidFeeDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _fees.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductPostpaidFeeDto>> GetById(long id)
        {
            var item = await _fees.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductPostpaidFeeDto>> Create(CreateProductPostpaidFeeRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product fees.");

            try
            {
                var created = await _fees.CreateAsync(request, CurrentUser);
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

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductPostpaidFeeDto>> Update(long id, UpdateProductPostpaidFeeRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product fees.");

            try
            {
                return Ok(await _fees.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool deleteBillers = false)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product fees.");

            try
            {
                await _fees.DeleteAsync(id, CurrentUser, deleteBillers);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
