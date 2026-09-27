using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteFailoverConfigs;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteFailoverConfigs
{
    public class RouteFailoverConfigService : IRouteFailoverConfigService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteFailoverConfigService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteFailoverConfigDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesFailoverConfig.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r => r.routing_type.ToLower().Contains(f) || (r.inst_id ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.routing_type).ThenBy(r => r.inst_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<RouteFailoverConfigDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RoutesFailoverConfig.AsNoTracking().FirstOrDefaultAsync(r => r.id == id);
            return entity == null ? null : ToDto(entity);
        }

        private static readonly string[] RoutingTypes = ["MARGIN", "PRODUCT"];

        public async Task<RouteFailoverConfigDto> CreateAsync(CreateRouteFailoverConfigRequest request, string actingUser)
        {
            if (!RoutingTypes.Contains(request.RoutingType))
                throw new ValidationException("Routing type harus MARGIN (topup) atau PRODUCT (bill payment).");

            if (await _context.RoutesFailoverConfig.AsNoTracking()
                .AnyAsync(r => r.routing_type == request.RoutingType && r.inst_id == request.InstId))
                throw new ConflictException($"Config untuk '{request.RoutingType}' / '{request.InstId ?? "(global)"}' sudah ada.");

            var entity = new SwRoutesFailoverConfig
            {
                routing_type = request.RoutingType,
                inst_id = request.InstId
            };
            Apply(entity, request, actingUser);

            _context.RoutesFailoverConfig.Add(entity);
            _audit.LogInsert(entity, "sw_routes_failover_config", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<RouteFailoverConfigDto> UpdateAsync(int id, UpdateRouteFailoverConfigRequest request, string actingUser)
        {
            var entity = await _context.RoutesFailoverConfig.FirstOrDefaultAsync(r => r.id == id)
                ?? throw new NotFoundException($"Failover config '{id}' not found.");

            var before = ToDto(entity);
            Apply(entity, request, actingUser);

            _audit.LogUpdate(before, ToDto(entity), "sw_routes_failover_config", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.RoutesFailoverConfig.FirstOrDefaultAsync(r => r.id == id)
                ?? throw new NotFoundException($"Failover config '{id}' not found.");

            _context.RoutesFailoverConfig.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_failover_config", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<FailoverSwitchDto>> GetSwitchesAsync()
        {
            var globals = await _context.RoutesFailoverConfig.AsNoTracking()
                .Where(r => r.inst_id == null && RoutingTypes.Contains(r.routing_type))
                .ToListAsync();

            return RoutingTypes.Select(t =>
            {
                var row = globals.FirstOrDefault(r => r.routing_type == t);
                return new FailoverSwitchDto(t, row == null || row.is_active == "1", row != null);
            }).ToList();
        }

        // tombol darurat per kategori = is_active baris config global. barisnya dibuat bila belum ada.
        public async Task<FailoverSwitchDto> SetSwitchAsync(string routingType, bool enabled, string actingUser)
        {
            if (!RoutingTypes.Contains(routingType))
                throw new ValidationException("Routing type harus MARGIN (topup) atau PRODUCT (bill payment).");

            var row = await _context.RoutesFailoverConfig
                .FirstOrDefaultAsync(r => r.routing_type == routingType && r.inst_id == null);

            if (row == null)
            {
                row = new SwRoutesFailoverConfig
                {
                    routing_type = routingType,
                    is_active = enabled ? "1" : "0",
                    updated_by = actingUser,
                    updated_dt = LocalClock.Now
                };

                _context.RoutesFailoverConfig.Add(row);
                _audit.LogInsert(row, "sw_routes_failover_config", actingUser);
            }
            else
            {
                var before = ToDto(row);
                row.is_active = enabled ? "1" : "0";
                row.updated_by = actingUser;
                row.updated_dt = LocalClock.Now;

                _audit.LogUpdate(before, ToDto(row), "sw_routes_failover_config", actingUser);
            }

            await _context.SaveChangesAsync();
            return new FailoverSwitchDto(routingType, enabled, true);
        }

        // validasi + normalisasi semua ambang. satu rc boleh ada di beberapa daftar: setiap
        // kategori yang memuatnya ikut menghitung (keputusan fase 2).
        private static void Apply(SwRoutesFailoverConfig e, UpdateRouteFailoverConfigRequest r, string actingUser)
        {
            e.rc_link_down = NormalizeRcList(r.RcLinkDown, "RC Link Down", required: true)!;
            e.link_down_cooldown_minutes = r.LinkDownCooldownMinutes;

            e.rc_suspect = NormalizeRcList(r.RcSuspect, "RC Timeout", required: true)!;
            e.max_consecutive_suspect = r.MaxConsecutiveSuspect;
            e.suspect_cooldown_minutes = r.SuspectCooldownMinutes;

            e.rc_failed = NormalizeRcList(r.RcFailed, "RC Gagal", required: false);
            e.max_consecutive_failed = r.MaxConsecutiveFailed;
            e.failed_cooldown_minutes = r.FailedCooldownMinutes;

            e.rc_pending = NormalizeRcList(r.RcPending, "RC Pending", required: false);
            e.max_consecutive_pending = r.MaxConsecutivePending;
            e.pending_cooldown_minutes = r.PendingCooldownMinutes;

            e.latency_threshold_ms = SecondsToMs(r.LatencyThresholdSeconds);
            e.max_consecutive_latency = r.MaxConsecutiveLatency;
            e.latency_cooldown_minutes = r.LatencyCooldownMinutes;

            if (new[] { e.max_consecutive_suspect, e.max_consecutive_failed, e.max_consecutive_pending, e.max_consecutive_latency }
                .Any(n => n < 1 || n > 100))
                throw new ValidationException("Max consecutive harus 1-100.");

            if (new[] { e.link_down_cooldown_minutes, e.suspect_cooldown_minutes, e.failed_cooldown_minutes,
                        e.pending_cooldown_minutes, e.latency_cooldown_minutes }.Any(m => m.HasValue && (m < 1 || m > 100000)))
                throw new ValidationException("Cooldown harus 1-100000 menit, atau kosong untuk reset manual.");

            e.is_active = r.IsActive ? "1" : "0";
            e.updated_by = actingUser;
            e.updated_dt = LocalClock.Now;
        }

        // "91, 1091,,89" -> "91,1091,89". rc 2-4 karakter huruf/angka, tanpa duplikat.
        // kosong -> null (kategori nonaktif) kecuali required.
        private static string? NormalizeRcList(string? value, string label, bool required)
        {
            var codes = (value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (codes.Length == 0)
            {
                if (required) throw new ValidationException($"{label} wajib diisi.");
                return null;
            }

            var invalid = codes.FirstOrDefault(c => c.Length < 2 || c.Length > 4 || !c.All(char.IsLetterOrDigit));
            if (invalid != null)
                throw new ValidationException($"{label}: rc '{invalid}' tidak valid (2-4 huruf/angka).");

            var duplicate = codes.GroupBy(c => c).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new ValidationException($"{label}: rc '{duplicate.Key}' ditulis lebih dari sekali.");

            var normalized = string.Join(",", codes);
            if (normalized.Length > 50)
                throw new ValidationException($"{label}: maksimal 50 karakter.");

            return normalized;
        }

        private static int? SecondsToMs(decimal? seconds)
        {
            if (!seconds.HasValue) return null;
            if (seconds.Value < 1 || seconds.Value > 300)
                throw new ValidationException("Ambang latency harus 1-300 detik.");

            return (int)Math.Round(seconds.Value * 1000m, MidpointRounding.AwayFromZero);
        }

        private static decimal? MsToSeconds(int? ms) => ms.HasValue ? ms.Value / 1000m : null;

        private static RouteFailoverConfigDto ToDto(SwRoutesFailoverConfig e) => new(
            e.id, e.routing_type, e.inst_id, e.rc_link_down, e.rc_suspect, e.max_consecutive_suspect,
            e.link_down_cooldown_minutes, e.suspect_cooldown_minutes, e.is_active == "1",
            e.rc_failed, e.max_consecutive_failed, e.failed_cooldown_minutes,
            e.rc_pending, e.max_consecutive_pending, e.pending_cooldown_minutes,
            MsToSeconds(e.latency_threshold_ms), e.max_consecutive_latency, e.latency_cooldown_minutes);
    }
}
