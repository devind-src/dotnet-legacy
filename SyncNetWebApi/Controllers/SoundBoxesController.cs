using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.SoundBoxes;
using SyncNetApi.Services.SoundBoxes;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/soundboxes")]
    public class SoundBoxesController : PermissionGatedControllerBase
    {
        private readonly ISoundBoxService _soundBoxes;

        public SoundBoxesController(ISoundBoxService soundBoxes, IUserService users) : base(users)
        {
            _soundBoxes = soundBoxes;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SoundBoxDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _soundBoxes.GetRecordsAsync(filter));

        [HttpGet("{nmid}")]
        public async Task<ActionResult<SoundBoxDto>> GetById(string nmid)
        {
            var soundBox = await _soundBoxes.GetByIdAsync(nmid);
            return soundBox == null ? NotFound() : Ok(soundBox);
        }

        [HttpPost]
        public async Task<ActionResult<SoundBoxDto>> Create(CreateSoundBoxRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add soundboxes.");

            try
            {
                var created = await _soundBoxes.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { nmid = created.Nmid }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "SoundBox already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{nmid}")]
        public async Task<ActionResult<SoundBoxDto>> Update(string nmid, UpdateSoundBoxRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit soundboxes.");

            try
            {
                return Ok(await _soundBoxes.UpdateAsync(nmid, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "SoundBox not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{nmid}")]
        public async Task<IActionResult> Delete(string nmid)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete soundboxes.");

            try
            {
                await _soundBoxes.DeleteAsync(nmid, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "SoundBox not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
