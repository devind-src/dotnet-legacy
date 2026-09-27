using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardProductLimits;
using SyncNetApi.Services.CardProductLimits;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-product-limits")]
    public class CardProductLimitsController : PermissionGatedControllerBase
    {
        private readonly ICardProductLimitService _limits;

        public CardProductLimitsController(ICardProductLimitService limits, IUserService users) : base(users)
        {
            _limits = limits;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardProductLimitDto>>> GetByProduct([FromQuery] int productId)
            => Ok(await _limits.GetByProductIdAsync(productId));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CardProductLimitDto>> GetById(int id)
        {
            var item = await _limits.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardProductLimitDto>> Create(CreateCardProductLimitRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product limits.");

            try
            {
                var created = await _limits.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid product limit", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CardProductLimitDto>> Update(int id, UpdateCardProductLimitRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product limits.");

            try
            {
                return Ok(await _limits.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product limits.");

            try
            {
                await _limits.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
