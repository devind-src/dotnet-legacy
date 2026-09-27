using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.HsmDevices;
using SyncNetApi.Services.HsmDevices;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/hsm-devices")]
    public class HsmDevicesController : PermissionGatedControllerBase
    {
        private readonly IHsmDeviceService _devices;

        public HsmDevicesController(IHsmDeviceService devices, IUserService users) : base(users)
        {
            _devices = devices;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<HsmDeviceDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _devices.GetRecordsAsync(filter));

        [HttpGet("{hsmName}")]
        public async Task<ActionResult<HsmDeviceDto>> GetById(string hsmName)
        {
            var item = await _devices.GetByIdAsync(hsmName);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<HsmDeviceDto>> Create(CreateHsmDeviceRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add HSM devices.");

            try
            {
                var created = await _devices.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { hsmName = created.HsmName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "HSM device already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{hsmName}")]
        public async Task<ActionResult<HsmDeviceDto>> Update(string hsmName, UpdateHsmDeviceRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit HSM devices.");

            try
            {
                return Ok(await _devices.UpdateAsync(hsmName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "HSM device not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{hsmName}")]
        public async Task<IActionResult> Delete(string hsmName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete HSM devices.");

            try
            {
                await _devices.DeleteAsync(hsmName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "HSM device not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
