using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Apps;
using SyncNetApi.Services.Apps;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/apps")]
    public class AppsController : PermissionGatedControllerBase
    {
        private readonly IAppService _apps;

        public AppsController(IAppService apps, IUserService users) : base(users)
        {
            _apps = apps;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AppDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _apps.GetRecordsAsync(filter));

        [HttpGet("new-command-port")]
        public async Task<ActionResult<int>> GetNewCommandPort() => Ok(await _apps.GetNewCommandPortAsync());

        [HttpGet("{appName}")]
        public async Task<ActionResult<AppDto>> GetById(string appName)
        {
            var item = await _apps.GetByIdAsync(appName);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<AppDto>> Create(CreateAppRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add applications.");

            try
            {
                var created = await _apps.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { appName = created.AppName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Application conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{appName}")]
        public async Task<ActionResult<AppDto>> Update(string appName, UpdateAppRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit applications.");

            try
            {
                return Ok(await _apps.UpdateAsync(appName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Application not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{appName}")]
        public async Task<IActionResult> Delete(string appName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete applications.");

            try
            {
                await _apps.DeleteAsync(appName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Application not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
