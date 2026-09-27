using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardOverrideLimits;
using SyncNetApi.Services.CardOverrideLimits;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-override-limits")]
    public class CardOverrideLimitsController : PermissionGatedControllerBase
    {
        private readonly ICardOverrideLimitService _limits;

        public CardOverrideLimitsController(ICardOverrideLimitService limits, IUserService users) : base(users)
        {
            _limits = limits;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardOverrideLimitDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _limits.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CardOverrideLimitDto>> GetById(int id)
        {
            var item = await _limits.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardOverrideLimitDto>> Create(CreateCardOverrideLimitRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add override limits.");

            var created = await _limits.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CardOverrideLimitDto>> Update(int id, UpdateCardOverrideLimitRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit override limits.");

            try
            {
                return Ok(await _limits.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Override limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete override limits.");

            try
            {
                await _limits.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Override limit not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
