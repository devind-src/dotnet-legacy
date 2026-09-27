using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.HsmServices;
using SyncNetApi.Services.HsmServices;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/hsm-services")]
    public class HsmServicesController : PermissionGatedControllerBase
    {
        private readonly IHsmServiceService _services;

        public HsmServicesController(IHsmServiceService services, IUserService users) : base(users)
        {
            _services = services;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<HsmServiceDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _services.GetRecordsAsync(filter));

        [HttpGet("{hsmDesc}")]
        public async Task<ActionResult<HsmServiceDto>> GetById(string hsmDesc)
        {
            var item = await _services.GetByIdAsync(hsmDesc);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<HsmServiceDto>> Create(CreateHsmServiceRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add HSM services.");

            try
            {
                var created = await _services.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { hsmDesc = created.HsmDesc }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "HSM service already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{hsmDesc}")]
        public async Task<ActionResult<HsmServiceDto>> Update(string hsmDesc, UpdateHsmServiceRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit HSM services.");

            try
            {
                return Ok(await _services.UpdateAsync(hsmDesc, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "HSM service not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{hsmDesc}")]
        public async Task<IActionResult> Delete(string hsmDesc)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete HSM services.");

            try
            {
                await _services.DeleteAsync(hsmDesc, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "HSM service not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
