using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardAccounts;
using SyncNetApi.Services.CardAccounts;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-accounts")]
    public class CardAccountsController : PermissionGatedControllerBase
    {
        private readonly ICardAccountService _cardAccounts;

        public CardAccountsController(ICardAccountService cardAccounts, IUserService users) : base(users)
        {
            _cardAccounts = cardAccounts;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardAccountDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cardAccounts.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CardAccountDto>> GetById(int id)
        {
            var item = await _cardAccounts.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardAccountDto>> Create(CreateCardAccountRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add card accounts.");

            try
            {
                var created = await _cardAccounts.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid card account", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CardAccountDto>> Update(int id, UpdateCardAccountRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit card accounts.");

            try
            {
                return Ok(await _cardAccounts.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete card accounts.");

            try
            {
                await _cardAccounts.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
