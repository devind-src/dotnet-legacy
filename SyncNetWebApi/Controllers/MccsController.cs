using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Mccs;
using SyncNetApi.Services.Mccs;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/mccs")]
    public class MccsController : PermissionGatedControllerBase
    {
        private readonly IMccService _mccs;

        public MccsController(IMccService mccs, IUserService users) : base(users)
        {
            _mccs = mccs;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MccDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _mccs.GetRecordsAsync(filter));

        [HttpGet("{mccCode}")]
        public async Task<ActionResult<MccDto>> GetById(string mccCode)
        {
            var item = await _mccs.GetByIdAsync(mccCode);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<MccDto>> Create(CreateMccRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add MCC codes.");

            try
            {
                var created = await _mccs.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { mccCode = created.MccCode }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "MCC already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{mccCode}")]
        public async Task<ActionResult<MccDto>> Update(string mccCode, UpdateMccRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit MCC codes.");

            try
            {
                return Ok(await _mccs.UpdateAsync(mccCode, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "MCC not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{mccCode}")]
        public async Task<IActionResult> Delete(string mccCode)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete MCC codes.");

            try
            {
                await _mccs.DeleteAsync(mccCode, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "MCC not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
