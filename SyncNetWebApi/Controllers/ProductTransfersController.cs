using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductTransfers;
using SyncNetApi.Services.ProductTransfers;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/master/transfers")]
    public class ProductTransfersController : PermissionGatedControllerBase
    {
        private readonly IProductTransferService _transfers;

        public ProductTransfersController(IProductTransferService transfers, IUserService users) : base(users)
        {
            _transfers = transfers;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductTransferDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _transfers.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductTransferDto>> GetById(long id)
        {
            var item = await _transfers.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductTransferDto>> Create(CreateProductTransferRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product transfers.");

            try
            {
                var created = await _transfers.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Product transfer already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductTransferDto>> Update(long id, UpdateProductTransferRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product transfers.");

            try
            {
                return Ok(await _transfers.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product transfer not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product transfers.");

            try
            {
                await _transfers.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product transfer not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
