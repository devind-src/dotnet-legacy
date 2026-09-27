using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteFailoverConfigs;
using SyncNetApi.Services.RouteFailoverConfigs;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/route-failover-configs")]
    public class RouteFailoverConfigsController : PermissionGatedControllerBase
    {
        private readonly IRouteFailoverConfigService _configs;

        public RouteFailoverConfigsController(IRouteFailoverConfigService configs, IUserService users) : base(users)
        {
            _configs = configs;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteFailoverConfigDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _configs.GetRecordsAsync(filter));

        /// <summary>Tombol darurat failover per kategori (MARGIN = topup, PRODUCT = bill payment).</summary>
        [HttpGet("switches")]
        public async Task<ActionResult<IReadOnlyList<FailoverSwitchDto>>> GetSwitches()
            => Ok(await _configs.GetSwitchesAsync());

        [HttpPut("switches/{routingType}")]
        public async Task<ActionResult<FailoverSwitchDto>> SetSwitch(string routingType, SetFailoverSwitchRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to change the failover switch.");

            try
            {
                return Ok(await _configs.SetSwitchAsync(routingType.ToUpperInvariant(), request.Enabled, CurrentUser));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RouteFailoverConfigDto>> GetById(int id)
        {
            var item = await _configs.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<RouteFailoverConfigDto>> Create(CreateRouteFailoverConfigRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add failover config.");

            try
            {
                var created = await _configs.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Config already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<RouteFailoverConfigDto>> Update(int id, UpdateRouteFailoverConfigRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit failover config.");

            try
            {
                return Ok(await _configs.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Config not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
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
                return Forbidden("You do not have permission to delete failover config.");

            try
            {
                await _configs.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Config not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
