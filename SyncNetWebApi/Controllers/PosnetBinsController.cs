using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.PosnetBins;
using SyncNetApi.Services.PosnetBins;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/posnet-bins")]
    public class PosnetBinsController : PermissionGatedControllerBase
    {
        private readonly IPosnetBinService _bins;

        public PosnetBinsController(IPosnetBinService bins, IUserService users) : base(users)
        {
            _bins = bins;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PosnetBinDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _bins.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PosnetBinDto>> GetById(int id)
        {
            var bin = await _bins.GetByIdAsync(id);
            return bin == null ? NotFound() : Ok(bin);
        }

        [HttpPost]
        public async Task<ActionResult<PosnetBinDto>> Create(CreatePosnetBinRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add BINs.");

            try
            {
                var created = await _bins.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PosnetBinDto>> Update(int id, UpdatePosnetBinRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit BINs.");

            try
            {
                return Ok(await _bins.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete BINs.");

            try
            {
                await _bins.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "BIN not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
