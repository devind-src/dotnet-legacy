using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CashoutMiniAtms;
using SyncNetApi.Services.CashoutMiniAtms;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/cashout-mini-atms")]
    public class CashoutMiniAtmsController : PermissionGatedControllerBase
    {
        private readonly ICashoutMiniAtmService _miniAtms;

        public CashoutMiniAtmsController(ICashoutMiniAtmService miniAtms, IUserService users) : base(users)
        {
            _miniAtms = miniAtms;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CashoutMiniAtmDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _miniAtms.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<CashoutMiniAtmDto>> GetById(long id)
        {
            var item = await _miniAtms.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CashoutMiniAtmDto>> Create(CreateCashoutMiniAtmRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add cashout Fee Mini ATM accounts.");

            try
            {
                var created = await _miniAtms.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<CashoutMiniAtmDto>> Update(long id, UpdateCashoutMiniAtmRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit cashout Fee Mini ATM accounts.");

            try
            {
                return Ok(await _miniAtms.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Cashout Fee Mini ATM account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
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
                return Forbidden("You do not have permission to delete cashout Fee Mini ATM accounts.");

            try
            {
                await _miniAtms.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Cashout Fee Mini ATM account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
