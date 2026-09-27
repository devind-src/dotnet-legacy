using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;
using SyncNetApi.Services.Queries;

namespace SyncNetApi.Controllers
{
    /// <summary>Read-only pages for menu "Queries" (dashboard_menu menu_id 8000-8002:
    /// Transaction/Audit Trail/User Log — Card, menu_id not covered here, is out of scope).
    /// Controller-level [Authorize] (no Roles=) rather than [Authorize(Roles = "admin")] like
    /// every CRUD module's controller (see PermissionGatedControllerBase) — same reasoning as
    /// MonitoringController (§7.23): nothing here mutates anything, so there's no
    /// CanAdd/CanEdit/CanDelete to gate. But unlike Monitoring's Realtime pages, the three
    /// Queries menus do NOT share one uniform role grant in dashboard_role_menu: Transaction is
    /// admin/spv/ops/mon (i.e. every role that exists), while Audit Trail and User Log are
    /// admin/spv only. Since "every role that exists" is the same as "any authenticated user"
    /// in this app, Transaction stays on the bare controller-level [Authorize] — but Audit
    /// Trail/User Log actions below add an explicit [Authorize(Roles = "admin,spv")] override
    /// so an ops/mon caller can't reach them directly even though they're technically
    /// authenticated (comma-separated Roles is OR logic in ASP.NET Core — admin OR spv, not
    /// both required).</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/queries")]
    public class QueriesController : ControllerBase
    {
        private readonly IQueryTransactionService _transactions;
        private readonly IQueryAuditService _audits;
        private readonly IQueryUserLogService _userLogs;

        public QueriesController(IQueryTransactionService transactions, IQueryAuditService audits, IQueryUserLogService userLogs)
        {
            _transactions = transactions;
            _audits = audits;
            _userLogs = userLogs;
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<PagedResultDto<TransactionListItemDto>>> SearchTransactions([FromQuery] TransactionSearchRequest request)
            => Ok(await _transactions.SearchAsync(request));

        [HttpGet("transactions/{tranNr:long}")]
        public async Task<ActionResult<TransactionDetailDto>> GetTransaction(long tranNr)
        {
            var item = await _transactions.GetDetailAsync(tranNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("audits")]
        [Authorize(Roles = "admin,spv")]
        public async Task<ActionResult<PagedResultDto<AuditListItemDto>>> SearchAudits([FromQuery] AuditSearchRequest request)
            => Ok(await _audits.SearchAsync(request));

        [HttpGet("audits/{id:long}")]
        [Authorize(Roles = "admin,spv")]
        public async Task<ActionResult<AuditDetailDto>> GetAudit(long id)
        {
            var item = await _audits.GetDetailAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("user-logs")]
        [Authorize(Roles = "admin,spv")]
        public async Task<ActionResult<PagedResultDto<UserLogItemDto>>> SearchUserLogs([FromQuery] UserLogSearchRequest request)
            => Ok(await _userLogs.SearchAsync(request));
    }
}
