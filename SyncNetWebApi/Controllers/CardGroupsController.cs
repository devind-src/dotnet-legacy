using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardGroups;
using SyncNetApi.Services.CardGroups;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-groups")]
    public class CardGroupsController : PermissionGatedControllerBase
    {
        private readonly ICardGroupService _cardGroups;

        public CardGroupsController(ICardGroupService cardGroups, IUserService users) : base(users)
        {
            _cardGroups = cardGroups;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardGroupDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cardGroups.GetRecordsAsync(filter));

        [HttpGet("{groupId:int}")]
        public async Task<ActionResult<CardGroupDto>> GetById(int groupId)
        {
            var item = await _cardGroups.GetByIdAsync(groupId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardGroupDto>> Create(CreateCardGroupRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add card groups.");

            var created = await _cardGroups.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { groupId = created.GroupId }, created);
        }

        [HttpPut("{groupId:int}")]
        public async Task<ActionResult<CardGroupDto>> Update(int groupId, UpdateCardGroupRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit card groups.");

            try
            {
                return Ok(await _cardGroups.UpdateAsync(groupId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{groupId:int}")]
        public async Task<IActionResult> Delete(int groupId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete card groups.");

            try
            {
                await _cardGroups.DeleteAsync(groupId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Card group is in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
