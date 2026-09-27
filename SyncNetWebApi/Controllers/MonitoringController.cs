using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Monitoring;
using SyncNetApi.Services.Monitoring;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Read-only pages for menu "Monitoring" (dashboard_menu menu_id 7001-7202:
    /// Realtime status, §7101/7102 Loggers below) plus Send Command (Application/Interface —
    /// see bottom of file). Deliberately [Authorize] rather than [Authorize(Roles = "admin")]
    /// like every other module's controller (see PermissionGatedControllerBase): the "mon" role
    /// exists specifically to view this menu (dashboard_role_menu grants it to
    /// admin/spv/ops/mon). Most actions here are plain reads with nothing to gate; Send Command
    /// is the one exception (see below) — this class can't inherit
    /// PermissionGatedControllerBase for that gate because its class-level
    /// [Authorize(Roles = "admin")] would defeat the whole point of this controller being open
    /// to non-admin Monitoring roles, so the same CurrentUser/CanEdit check is duplicated here
    /// in miniature instead. Confirmed with user before building (2026-09-09).</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/monitoring")]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoring;
        private readonly IMonitoringLogService _logs;
        private readonly IMonitoringCommandService _commands;
        private readonly IUserService _users;

        public MonitoringController(IMonitoringService monitoring, IMonitoringLogService logs, IMonitoringCommandService commands, IUserService users)
        {
            _monitoring = monitoring;
            _logs = logs;
            _commands = commands;
            _users = users;
        }

        [HttpGet("applications")]
        public async Task<ActionResult<IReadOnlyList<ApplicationMonitorDto>>> GetApplications([FromQuery] string? filter)
            => Ok(await _monitoring.GetApplicationsAsync(filter));

        [HttpGet("interfaces")]
        public async Task<ActionResult<IReadOnlyList<InterfaceMonitorDto>>> GetInterfaces([FromQuery] string? filter)
            => Ok(await _monitoring.GetInterfacesAsync(filter));

        [HttpGet("connections")]
        public async Task<ActionResult<IReadOnlyList<ConnectionMonitorDto>>> GetConnections([FromQuery] string? filter)
            => Ok(await _monitoring.GetConnectionsAsync(filter));

        [HttpGet("terminals")]
        public async Task<ActionResult<IReadOnlyList<TerminalMonitorDto>>> GetTerminals([FromQuery] string? filter)
            => Ok(await _monitoring.GetTerminalsAsync(filter));

        [HttpGet("services")]
        public async Task<ActionResult<IReadOnlyList<ServiceMonitorDto>>> GetServices([FromQuery] string? filter)
            => Ok(await _monitoring.GetServicesAsync(filter));

        // -----------------------------------------------------------------------
        // Send Command — ports legacy Application/Detail.razor and Nodes/Detail.razor. Opens a
        // raw TCP connection to a live switch process and sends an operator-chosen command
        // (VERSION/RESYNC/TRACE ON-OFF for Application; VERSION/ECHO/SIGNON/SIGNOFF/KEYCHANGE/
        // OTHER for Interface — SIGNOFF/KEYCHANGE can disrupt a live connection). Gated on
        // CanEdit (the same global Add/Edit/Delete permission model every mutating action in
        // this app uses, see PROJECT_TECHNICAL_SUMMARY.md §4) rather than left open to every
        // Monitoring-menu role like the GET endpoints above — sending a command has real
        // operational side effects, reading a status list does not. Implemented per explicit
        // user request (2026-09-09); the bulk "Resync Config" page and Service Start/Stop/Reset
        // were NOT requested and remain out of scope.
        // -----------------------------------------------------------------------

        [HttpPost("applications/{appName}/command")]
        public async Task<ActionResult<CommandResponseDto>> SendApplicationCommand(string appName, SendApplicationCommandRequest request)
        {
            if (!await CanSendCommandAsync())
                return Forbidden("You do not have permission to send commands.");

            try
            {
                var response = await _commands.SendApplicationCommandAsync(appName, request.Command);
                return Ok(new CommandResponseDto(response));
            }
            catch (NotFoundException ex) { return NotFound(new ProblemDetails { Title = "Application not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound }); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid command", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
        }

        [HttpPost("interfaces/{nodeId:int}/command")]
        public async Task<ActionResult<CommandResponseDto>> SendInterfaceCommand(int nodeId, SendNodeCommandRequest request)
        {
            if (!await CanSendCommandAsync())
                return Forbidden("You do not have permission to send commands.");

            try
            {
                var response = await _commands.SendInterfaceCommandAsync(nodeId, request.Command, request.OtherCommand);
                return Ok(new CommandResponseDto(response));
            }
            catch (NotFoundException ex) { return NotFound(new ProblemDetails { Title = "Node not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound }); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid command", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
        }

        private string CurrentUser => User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "system";

        private async Task<bool> CanSendCommandAsync()
        {
            try
            {
                var permissions = await _users.GetEffectivePermissionsAsync(CurrentUser);
                return permissions.CanEdit;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        private static ObjectResult Forbidden(string detail) => new(new ProblemDetails
        {
            Title = "Forbidden",
            Detail = detail,
            Status = StatusCodes.Status403Forbidden
        })
        { StatusCode = StatusCodes.Status403Forbidden };

        // -----------------------------------------------------------------------
        // Jobs (menu_id 7201/7202): reuse sw_jobs (Configuration > Jobs > Job Schedule) and
        // sw_job_logs (read-only, no CRUD elsewhere in the app).
        // -----------------------------------------------------------------------

        [HttpGet("jobs")]
        public async Task<ActionResult<IReadOnlyList<JobMonitorDto>>> GetJobs([FromQuery] string? filter)
            => Ok(await _monitoring.GetJobsAsync(filter));

        [HttpGet("job-logs")]
        public async Task<ActionResult<IReadOnlyList<JobLogMonitorDto>>> GetJobLogs([FromQuery] string? filter)
            => Ok(await _monitoring.GetJobLogsAsync(filter));

        // -----------------------------------------------------------------------
        // Loggers — Log Viewer (menu_id 7101): flat folder, files named
        // "{AppName}_{date}.log".
        // -----------------------------------------------------------------------

        [HttpGet("logs/apps")]
        public ActionResult<IReadOnlyList<string>> GetLogApps()
            => Ok(_logs.GetLogAppNames());

        [HttpGet("logs/files")]
        public ActionResult<IReadOnlyList<MonitoringFileDto>> GetLogFiles([FromQuery] string app)
        {
            try { return Ok(_logs.GetLogFiles(app)); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid app", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
        }

        [HttpGet("logs/content")]
        public ActionResult<MonitoringFileContentDto> GetLogContent([FromQuery] string file, [FromQuery] int offset = 0, [FromQuery] int pageSize = 0)
        {
            try { return Ok(_logs.GetLogContent(file, offset, pageSize)); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid file", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
            catch (NotFoundException ex) { return NotFound(new ProblemDetails { Title = "Log file not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound }); }
        }

        // -----------------------------------------------------------------------
        // Loggers — Trace Viewer (menu_id 7102): one real subfolder per app.
        // -----------------------------------------------------------------------

        [HttpGet("traces/apps")]
        public ActionResult<IReadOnlyList<string>> GetTraceApps()
            => Ok(_logs.GetTraceAppNames());

        [HttpGet("traces/files")]
        public ActionResult<IReadOnlyList<MonitoringFileDto>> GetTraceFiles([FromQuery] string app)
        {
            try { return Ok(_logs.GetTraceFiles(app)); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid app", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
        }

        [HttpGet("traces/content")]
        public ActionResult<MonitoringFileContentDto> GetTraceContent(
            [FromQuery] string app, [FromQuery] string file, [FromQuery] int offset = 0,
            [FromQuery] int pageSize = 0, [FromQuery] string? search = null)
        {
            try { return Ok(_logs.GetTraceContent(app, file, offset, pageSize, search)); }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid app or file", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
            catch (NotFoundException ex) { return NotFound(new ProblemDetails { Title = "Trace file not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound }); }
        }

        [HttpGet("traces/download")]
        public IActionResult DownloadTraceFile([FromQuery] string app, [FromQuery] string file)
        {
            try
            {
                var (content, downloadFileName) = _logs.GetTraceFileZip(app, file);
                return File(content, "application/zip", downloadFileName);
            }
            catch (ValidationException ex) { return BadRequest(new ProblemDetails { Title = "Invalid app or file", Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
            catch (NotFoundException ex) { return NotFound(new ProblemDetails { Title = "Trace file not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound }); }
        }
    }
}
