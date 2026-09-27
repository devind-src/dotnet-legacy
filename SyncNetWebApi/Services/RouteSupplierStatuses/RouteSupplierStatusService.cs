using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteSupplierStatuses;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteSupplierStatuses
{
    public class RouteSupplierStatusService : IRouteSupplierStatusService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteSupplierStatusService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteSupplierStatusDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesSupplierStatus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => x.supplier_id.ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.supplier_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        // reset manual dari dashboard - override sebelum cooldown habis. kalau baris belum
        // ada (supplier belum pernah tercatat bermasalah), buat baris ACTIVE baru supaya
        // riwayat reset tetap tercatat.
        public async Task<RouteSupplierStatusDto> ResetAsync(string supplierId, string actingUser)
        {
            var entity = await _context.RoutesSupplierStatus.FirstOrDefaultAsync(x => x.supplier_id == supplierId);

            var isNew = entity == null;
            if (entity == null)
            {
                entity = new Entities.SwRoutesSupplierStatus { supplier_id = supplierId };
                _context.RoutesSupplierStatus.Add(entity);
            }

            var before = isNew ? null : ToDto(entity);

            entity.status = "ACTIVE";
            entity.consecutive_suspect_count = 0;
            entity.consecutive_failed_count = 0;
            entity.consecutive_pending_count = 0;
            entity.consecutive_latency_count = 0;
            entity.block_reason = null;
            entity.retry_count = 0;
            entity.blocked_since = null;
            entity.blocked_until = null;
            entity.updated_by = actingUser;
            entity.updated_dt = LocalClock.Now;

            if (isNew)
                _audit.LogInsert(entity, "sw_routes_supplier_status", actingUser);
            else
                _audit.LogUpdate(before, ToDto(entity), "sw_routes_supplier_status", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        private static RouteSupplierStatusDto ToDto(Entities.SwRoutesSupplierStatus e) => new(
            e.supplier_id, e.status, e.last_rc_code, e.consecutive_suspect_count, e.retry_count,
            e.last_tran_dt, e.blocked_since, e.blocked_until, e.updated_by, e.updated_dt,
            e.consecutive_failed_count, e.consecutive_pending_count, e.consecutive_latency_count,
            e.block_reason, e.last_latency_ms);
    }
}
