using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Dtos.RouteFailoverLogs;
using SyncNetApi.Services.RouteFailoverLogs;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Routing &gt; Failover Log (`/route-failover-log`) — read-only audit trail,
    /// written by the switching engine whenever a supplier transitions to DOWN/SUSPECT.</summary>
    [Route("api/v1/route-failover-logs")]
    public class RouteFailoverLogsController : PermissionGatedControllerBase
    {
        private readonly IRouteFailoverLogService _logs;

        public RouteFailoverLogsController(IRouteFailoverLogService logs, IUserService users) : base(users)
        {
            _logs = logs;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteFailoverLogDto>>> GetAll(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] string? instId, [FromQuery] string? supplierId, [FromQuery] string? reason,
            [FromQuery] string? routingType = null)
            => Ok(await _logs.GetRecordsAsync(from, to, instId, supplierId, reason, routingType));
    }
}
