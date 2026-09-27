using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.VaAccounts;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaAccounts;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/va-accounts")]
    public class VaAccountsController : PermissionGatedControllerBase
    {
        private readonly IVaAccountService _vaAccounts;

        public VaAccountsController(IVaAccountService vaAccounts, IUserService users) : base(users)
        {
            _vaAccounts = vaAccounts;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaAccountDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _vaAccounts.GetRecordsAsync(filter));

        [HttpGet("{accNr}")]
        public async Task<ActionResult<VaAccountDto>> GetById(string accNr)
        {
            var item = await _vaAccounts.GetByIdAsync(accNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<VaAccountDto>> Create(CreateVaAccountRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add VA accounts.");

            try
            {
                var created = await _vaAccounts.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { accNr = created.AccNr }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "VA account already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid VA account", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{accNr}")]
        public async Task<ActionResult<VaAccountDto>> Update(string accNr, UpdateVaAccountRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit VA accounts.");

            try
            {
                return Ok(await _vaAccounts.UpdateAsync(accNr, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "VA account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{accNr}")]
        public async Task<IActionResult> Delete(string accNr)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete VA accounts.");

            try
            {
                await _vaAccounts.DeleteAsync(accNr, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "VA account not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
