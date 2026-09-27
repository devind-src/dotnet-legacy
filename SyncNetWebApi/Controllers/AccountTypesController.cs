using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.AccountTypes;
using SyncNetApi.Services.AccountTypes;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/account-types")]
    public class AccountTypesController : PermissionGatedControllerBase
    {
        private readonly IAccountTypeService _accountTypes;

        public AccountTypesController(IAccountTypeService accountTypes, IUserService users) : base(users)
        {
            _accountTypes = accountTypes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AccountTypeDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _accountTypes.GetRecordsAsync(filter));

        [HttpGet("{acctType}")]
        public async Task<ActionResult<AccountTypeDto>> GetById(string acctType)
        {
            var item = await _accountTypes.GetByIdAsync(acctType);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<AccountTypeDto>> Create(CreateAccountTypeRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add account types.");

            try
            {
                var created = await _accountTypes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { acctType = created.AcctType }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Account type already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{acctType}")]
        public async Task<ActionResult<AccountTypeDto>> Update(string acctType, UpdateAccountTypeRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit account types.");

            try
            {
                return Ok(await _accountTypes.UpdateAsync(acctType, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Account type not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{acctType}")]
        public async Task<IActionResult> Delete(string acctType)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete account types.");

            try
            {
                await _accountTypes.DeleteAsync(acctType, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Account type not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
