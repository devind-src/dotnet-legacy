using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardBins;
using SyncNetApi.Services.CardBins;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-bins")]
    public class CardBinsController : PermissionGatedControllerBase
    {
        private readonly ICardBinService _cardBins;

        public CardBinsController(ICardBinService cardBins, IUserService users) : base(users)
        {
            _cardBins = cardBins;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardBinDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cardBins.GetRecordsAsync(filter));

        [HttpGet("{binNr}")]
        public async Task<ActionResult<CardBinDto>> GetById(string binNr)
        {
            var item = await _cardBins.GetByIdAsync(binNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardBinDto>> Create(CreateCardBinRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add card BINs.");

            try
            {
                var created = await _cardBins.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { binNr = created.BinNr }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid card BIN", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Card BIN already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{binNr}")]
        public async Task<ActionResult<CardBinDto>> Update(string binNr, UpdateCardBinRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit card BINs.");

            try
            {
                return Ok(await _cardBins.UpdateAsync(binNr, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{binNr}")]
        public async Task<IActionResult> Delete(string binNr)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete card BINs.");

            try
            {
                await _cardBins.DeleteAsync(binNr, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Card BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
