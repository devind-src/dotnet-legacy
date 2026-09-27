using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductVolumeTierings;
using SyncNetApi.Services.ProductVolumeTierings;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/pricing-fees/volume-tiering")]
    public class ProductVolumeTieringsController : PermissionGatedControllerBase
    {
        private readonly IProductVolumeTieringService _tierings;

        public ProductVolumeTieringsController(IProductVolumeTieringService tierings, IUserService users) : base(users)
        {
            _tierings = tierings;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductVolumeTieringDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _tierings.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductVolumeTieringDto>> GetById(long id)
        {
            var item = await _tierings.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductVolumeTieringDto>> Create(CreateProductVolumeTieringRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add tiering fees.");

            try
            {
                var created = await _tierings.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Tiering fee overlaps", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProductVolumeTieringDto>> Update(long id, UpdateProductVolumeTieringRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit tiering fees.");

            try
            {
                return Ok(await _tierings.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Tiering fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Tiering fee overlaps", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete tiering fees.");

            try
            {
                await _tierings.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Tiering fee not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
