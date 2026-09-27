using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.SubMerchantGroups;
using SyncNetApi.Services.SubMerchantGroups;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/submerchant-groups")]
    public class SubMerchantGroupsController : PermissionGatedControllerBase
    {
        private readonly ISubMerchantGroupService _groups;

        public SubMerchantGroupsController(ISubMerchantGroupService groups, IUserService users) : base(users)
        {
            _groups = groups;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SubMerchantGroupDto>>> GetAll([FromQuery] string? filter, [FromQuery] string? participantId)
        {
            var records = await _groups.GetRecordsAsync(filter);
            if (!string.IsNullOrWhiteSpace(participantId))
                records = records.Where(r => r.ParticipantId == participantId).ToList();
            return Ok(records);
        }

        [HttpGet("{groupName}")]
        public async Task<ActionResult<SubMerchantGroupDto>> GetByName(string groupName)
        {
            var group = await _groups.GetByNameAsync(groupName);
            return group == null ? NotFound() : Ok(group);
        }

        [HttpPost]
        public async Task<ActionResult<SubMerchantGroupDto>> Create(CreateSubMerchantGroupRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add sub merchant groups.");

            try
            {
                var created = await _groups.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetByName), new { groupName = created.GroupName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Sub merchant group already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{groupName}")]
        public async Task<ActionResult<SubMerchantGroupDto>> Update(string groupName, UpdateSubMerchantGroupRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit sub merchant groups.");

            try
            {
                return Ok(await _groups.UpdateAsync(groupName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Sub merchant group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{groupName}")]
        public async Task<IActionResult> Delete(string groupName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete sub merchant groups.");

            try
            {
                await _groups.DeleteAsync(groupName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Sub merchant group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Sub merchant group in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
