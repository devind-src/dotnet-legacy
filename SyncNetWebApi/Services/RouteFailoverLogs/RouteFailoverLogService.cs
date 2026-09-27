using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteFailoverConfigs;
using SyncNetApi.Dtos.RouteFailoverLogs;

namespace SyncNetApi.Services.RouteFailoverLogs
{
    public class RouteFailoverLogService : IRouteFailoverLogService
    {
        private readonly SyncNetDbContext _context;

        public RouteFailoverLogService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RouteFailoverLogDto>> GetRecordsAsync(DateTime? from = null, DateTime? to = null,
            string? instId = null, string? supplierId = null, string? reason = null, string? routingType = null)
        {
            var query = _context.RoutesFailoverLog.AsNoTracking().AsQueryable();

            if (from.HasValue) query = query.Where(x => x.created_dt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.created_dt <= to.Value);
            if (!string.IsNullOrWhiteSpace(instId)) query = query.Where(x => x.inst_id == instId);
            if (!string.IsNullOrWhiteSpace(supplierId))
                query = query.Where(x => x.from_supplier_id == supplierId || x.to_supplier_id == supplierId);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                // baris sebelum fase 2 mencatat timeout sebagai SUSPECT_TIMEOUT
                if (reason == HealthCheckReasons.Timeout)
                    query = query.Where(x => x.reason == reason || x.reason == HealthCheckReasons.LegacyTimeout);
                else
                    query = query.Where(x => x.reason == reason);
            }
            if (!string.IsNullOrWhiteSpace(routingType)) query = query.Where(x => x.routing_type == routingType);

            var list = await query.OrderByDescending(x => x.created_dt).Take(500).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        private static RouteFailoverLogDto ToDto(Entities.SwRoutesFailoverLog e) => new(
            e.id, e.routing_type, e.inst_id, e.denom, e.trace_number, e.from_supplier_id,
            e.to_supplier_id, e.rc_code, e.reason, e.created_dt, e.latency_ms, e.schedule_id);
    }
}
