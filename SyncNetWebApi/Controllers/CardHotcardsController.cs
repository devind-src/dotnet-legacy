using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardHotcards;
using SyncNetApi.Services.CardHotcards;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-hotcards")]
    public class CardHotcardsController : PermissionGatedControllerBase
    {
        private readonly ICardHotcardService _cardHotcards;

        public CardHotcardsController(ICardHotcardService cardHotcards, IUserService users) : base(users)
        {
            _cardHotcards = cardHotcards;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardHotcardDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cardHotcards.GetRecordsAsync(filter));

        [HttpGet("{cardNr}")]
        public async Task<ActionResult<CardHotcardDto>> GetById(string cardNr)
        {
            var item = await _cardHotcards.GetByIdAsync(cardNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardHotcardDto>> Create(CreateCardHotcardRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add hotcards.");

            try
            {
                var created = await _cardHotcards.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { cardNr = created.CardNr }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Hotcard already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{cardNr}")]
        public async Task<ActionResult<CardHotcardDto>> Update(string cardNr, UpdateCardHotcardRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit hotcards.");

            try
            {
                return Ok(await _cardHotcards.UpdateAsync(cardNr, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Hotcard not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{cardNr}")]
        public async Task<IActionResult> Delete(string cardNr)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete hotcards.");

            try
            {
                await _cardHotcards.DeleteAsync(cardNr, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Hotcard not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
