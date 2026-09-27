using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.TranNames;
using SyncNetApi.Services.TranNames;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/tran-names")]
    public class TranNamesController : PermissionGatedControllerBase
    {
        private readonly ITranNameService _tranNames;

        public TranNamesController(ITranNameService tranNames, IUserService users) : base(users)
        {
            _tranNames = tranNames;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TranNameDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _tranNames.GetRecordsAsync(filter));

        [HttpGet("{transCode}")]
        public async Task<ActionResult<TranNameDto>> GetById(string transCode)
        {
            var item = await _tranNames.GetByIdAsync(transCode);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<TranNameDto>> Create(CreateTranNameRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add tran names.");

            try
            {
                var created = await _tranNames.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { transCode = created.TransCode }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Tran name already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{transCode}")]
        public async Task<ActionResult<TranNameDto>> Update(string transCode, UpdateTranNameRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit tran names.");

            try
            {
                return Ok(await _tranNames.UpdateAsync(transCode, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Tran name not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{transCode}")]
        public async Task<IActionResult> Delete(string transCode)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete tran names.");

            try
            {
                await _tranNames.DeleteAsync(transCode, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Tran name not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
