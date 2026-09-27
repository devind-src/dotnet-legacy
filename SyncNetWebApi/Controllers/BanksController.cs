using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Banks;
using SyncNetApi.Services.Banks;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/banks")]
    public class BanksController : PermissionGatedControllerBase
    {
        private readonly IBankService _banks;

        public BanksController(IBankService banks, IUserService users) : base(users)
        {
            _banks = banks;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BankDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _banks.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BankDto>> GetById(int id)
        {
            var item = await _banks.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<BankDto>> Create(CreateBankRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add banks.");

            var created = await _banks.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BankDto>> Update(int id, UpdateBankRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit banks.");

            try
            {
                return Ok(await _banks.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Bank not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete banks.");

            try
            {
                await _banks.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Bank not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
