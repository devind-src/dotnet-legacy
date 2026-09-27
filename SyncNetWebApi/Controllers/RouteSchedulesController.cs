using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.RouteSchedules;
using SyncNetApi.Services.RouteSchedules;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Routing &gt; Jadwal Routing (`/route-schedule`), Fase 3 routing by priority time.</summary>
    [Route("api/v1/route-schedules")]
    public class RouteSchedulesController : PermissionGatedControllerBase
    {
        private readonly IRouteScheduleService _schedules;

        public RouteSchedulesController(IRouteScheduleService schedules, IUserService users) : base(users)
        {
            _schedules = schedules;
        }

        /// <summary>states: daftar status dipisah koma (UPCOMING,RUNNING,ACTIVE,FINISHED,CANCELLED); kosong = semua.</summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RouteScheduleDto>>> GetAll([FromQuery] string? filter,
            [FromQuery] string? states, [FromQuery] int? nodeId, [FromQuery] string? ruleType)
        {
            var stateList = (states ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToUpperInvariant()).ToList();
            return Ok(await _schedules.GetRecordsAsync(filter, stateList, nodeId, ruleType));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RouteScheduleDto>> GetById(int id)
        {
            var item = await _schedules.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<RouteScheduleHistDto>>> GetHistory(int id)
            => Ok(await _schedules.GetHistoryAsync(id));

        /// <summary>Cek Jadwal (dry-run): at = tanggal-jam lokal server; kosong = sekarang.</summary>
        [HttpGet("check")]
        public async Task<ActionResult<ScheduleCheckResultDto>> Check([FromQuery] DateTime? at, [FromQuery] string? productId,
            [FromQuery] int? nodeId, [FromQuery] int? denom)
        {
            var when = DateTime.SpecifyKind(at ?? LocalClock.Now, DateTimeKind.Unspecified);
            return Ok(await _schedules.CheckAsync(when, string.IsNullOrWhiteSpace(productId) ? null : productId.Trim(), nodeId, denom));
        }

        /// <summary>Dampak aturan yang sedang diisi di form (belum disimpan).</summary>
        [HttpPost("preview")]
        public async Task<ActionResult<ScheduleCheckResultDto>> Preview(SaveRouteScheduleRequest request, [FromQuery] int? editingId)
        {
            try
            {
                return Ok(await _schedules.PreviewAsync(request, editingId));
            }
            catch (ValidationException ex)
            {
                return BadRequest(Problem400(ex));
            }
        }

        [HttpPost]
        public async Task<ActionResult<RouteScheduleDto>> Create(SaveRouteScheduleRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add routing schedules.");

            try
            {
                var created = await _schedules.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(Problem400(ex));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<RouteScheduleDto>> Update(int id, SaveRouteScheduleRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit routing schedules.");

            try
            {
                return Ok(await _schedules.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(Problem404(ex));
            }
            catch (ValidationException ex)
            {
                return BadRequest(Problem400(ex));
            }
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<RouteScheduleDto>> Cancel(int id, CancelRouteScheduleRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to cancel routing schedules.");

            try
            {
                return Ok(await _schedules.CancelAsync(id, request.Reason, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(Problem404(ex));
            }
            catch (ValidationException ex)
            {
                return BadRequest(Problem400(ex));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete routing schedules.");

            try
            {
                await _schedules.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(Problem404(ex));
            }
            catch (ValidationException ex)
            {
                return BadRequest(Problem400(ex));
            }
        }

        private static ProblemDetails Problem400(Exception ex) =>
            new() { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest };

        private static ProblemDetails Problem404(Exception ex) =>
            new() { Title = "Schedule not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound };
    }
}
