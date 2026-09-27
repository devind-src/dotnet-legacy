using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Dtos.RouteSupplierStatuses;
using SyncNetApi.Services.RouteSupplierStatuses;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Routing &gt; Supplier Status (`/route-supplier-status`) — monitoring page,
    /// rows are written by the switching engine, not created from the dashboard. Only
    /// action available here is a manual "Reset ke Active" override.</summary>
    [Route("api/v1/route-supplier-statuses")]
    public class RouteSupplierStatusesController : PermissionGatedControllerBase
    {
        private readonly IRouteSupplierStatusService _status;

        public RouteSupplierStatusesController(IRouteSupplierStatusService status, IUserService users) : base(users)
        {
            _status = status;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteSupplierStatusDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _status.GetRecordsAsync(filter));

        [HttpPost("{supplierId}/reset")]
        public async Task<ActionResult<RouteSupplierStatusDto>> Reset(string supplierId)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to reset supplier status.");

            return Ok(await _status.ResetAsync(supplierId, CurrentUser));
        }
    }
}
