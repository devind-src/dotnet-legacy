using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Roles;
using SyncNetApi.Services.Roles;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/roles")]
    public class RolesController : PermissionGatedControllerBase
    {
        private readonly IRoleService _roles;

        public RolesController(IRoleService roles, IUserService users) : base(users)
        {
            _roles = roles;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _roles.GetRecordsAsync(filter));

        [HttpGet("{roleName}")]
        public async Task<ActionResult<RoleDetailDto>> GetByName(string roleName)
        {
            var role = await _roles.GetDetailAsync(roleName);
            return role == null ? NotFound() : Ok(role);
        }

        [HttpPost]
        public async Task<ActionResult<RoleDetailDto>> Create(CreateRoleRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add roles.");

            try
            {
                var created = await _roles.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetByName), new { roleName = created.RoleName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Role already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{roleName}")]
        public async Task<ActionResult<RoleDetailDto>> Update(string roleName, UpdateRoleRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit roles.");

            try
            {
                return Ok(await _roles.UpdateAsync(roleName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Role not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{roleName}")]
        public async Task<IActionResult> Delete(string roleName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete roles.");

            try
            {
                await _roles.DeleteAsync(roleName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Role not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Role in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
