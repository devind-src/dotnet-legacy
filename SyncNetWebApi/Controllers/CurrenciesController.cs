using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Currencies;
using SyncNetApi.Services.Currencies;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/currencies")]
    public class CurrenciesController : PermissionGatedControllerBase
    {
        private readonly ICurrencyService _currencies;

        public CurrenciesController(ICurrencyService currencies, IUserService users) : base(users)
        {
            _currencies = currencies;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _currencies.GetRecordsAsync(filter));

        [HttpGet("{currencyCode}")]
        public async Task<ActionResult<CurrencyDto>> GetById(string currencyCode)
        {
            var item = await _currencies.GetByIdAsync(currencyCode);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CurrencyDto>> Create(CreateCurrencyRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add currencies.");

            try
            {
                var created = await _currencies.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { currencyCode = created.CurrencyCode }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Currency already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{currencyCode}")]
        public async Task<ActionResult<CurrencyDto>> Update(string currencyCode, UpdateCurrencyRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit currencies.");

            try
            {
                return Ok(await _currencies.UpdateAsync(currencyCode, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Currency not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{currencyCode}")]
        public async Task<IActionResult> Delete(string currencyCode)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete currencies.");

            try
            {
                await _currencies.DeleteAsync(currencyCode, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Currency not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
