using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.VaGroups;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaGroups;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/va-groups")]
    public class VaGroupsController : PermissionGatedControllerBase
    {
        private readonly IVaGroupService _vaGroups;

        public VaGroupsController(IVaGroupService vaGroups, IUserService users) : base(users)
        {
            _vaGroups = vaGroups;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaGroupDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _vaGroups.GetRecordsAsync(filter));

        [HttpGet("{groupName}")]
        public async Task<ActionResult<VaGroupDto>> GetById(string groupName)
        {
            var item = await _vaGroups.GetByIdAsync(groupName);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<VaGroupDto>> Create(CreateVaGroupRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add VA groups.");

            try
            {
                var created = await _vaGroups.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { groupName = created.GroupName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "VA group already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{groupName}")]
        public async Task<ActionResult<VaGroupDto>> Update(string groupName, UpdateVaGroupRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit VA groups.");

            try
            {
                return Ok(await _vaGroups.UpdateAsync(groupName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "VA group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{groupName}")]
        public async Task<IActionResult> Delete(string groupName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete VA groups.");

            try
            {
                await _vaGroups.DeleteAsync(groupName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "VA group not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "VA group is in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
