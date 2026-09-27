using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CashoutWithdrawals;
using SyncNetApi.Services.CashoutWithdrawals;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/cashout-withdrawals")]
    public class CashoutWithdrawalsController : PermissionGatedControllerBase
    {
        private readonly ICashoutWithdrawalService _withdrawals;

        public CashoutWithdrawalsController(ICashoutWithdrawalService withdrawals, IUserService users) : base(users)
        {
            _withdrawals = withdrawals;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CashoutWithdrawalDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _withdrawals.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<CashoutWithdrawalDto>> GetById(long id)
        {
            var item = await _withdrawals.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CashoutWithdrawalDto>> Create(CreateCashoutWithdrawalRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add cashout withdrawal accounts.");

            try
            {
                var created = await _withdrawals.CreateAsync(request, CurrentUser);
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
        public async Task<ActionResult<CashoutWithdrawalDto>> Update(long id, UpdateCashoutWithdrawalRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit cashout withdrawal accounts.");

            try
            {
                return Ok(await _withdrawals.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Cashout withdrawal account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
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
                return Forbidden("You do not have permission to delete cashout withdrawal accounts.");

            try
            {
                await _withdrawals.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Cashout withdrawal account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
