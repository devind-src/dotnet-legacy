using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.SubMerchants;
using SyncNetApi.Services.SubMerchants;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/submerchants")]
    public class SubMerchantsController : PermissionGatedControllerBase
    {
        private readonly ISubMerchantService _subMerchants;

        public SubMerchantsController(ISubMerchantService subMerchants, IUserService users) : base(users)
        {
            _subMerchants = subMerchants;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SubMerchantDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _subMerchants.GetRecordsAsync(filter));

        [HttpGet("{subMerchantId}")]
        public async Task<ActionResult<SubMerchantDto>> GetById(string subMerchantId)
        {
            var subMerchant = await _subMerchants.GetByIdAsync(subMerchantId);
            return subMerchant == null ? NotFound() : Ok(subMerchant);
        }

        [HttpPost]
        public async Task<ActionResult<SubMerchantDto>> Create(CreateSubMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add sub merchants.");

            try
            {
                var created = await _subMerchants.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { subMerchantId = created.SubMerchantId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Sub merchant already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{subMerchantId}")]
        public async Task<ActionResult<SubMerchantDto>> Update(string subMerchantId, UpdateSubMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit sub merchants.");

            try
            {
                return Ok(await _subMerchants.UpdateAsync(subMerchantId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Sub merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{subMerchantId}")]
        public async Task<IActionResult> Delete(string subMerchantId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete sub merchants.");

            try
            {
                await _subMerchants.DeleteAsync(subMerchantId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Sub merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Sub merchant in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
