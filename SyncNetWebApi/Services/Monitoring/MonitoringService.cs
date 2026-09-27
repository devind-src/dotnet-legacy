using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Monitoring;

namespace SyncNetApi.Services.Monitoring
{
    public class MonitoringService : IMonitoringService
    {
        private readonly SyncNetDbContext _context;

        public MonitoringService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ApplicationMonitorDto>> GetApplicationsAsync(string? filter = null)
        {
            var query = _context.Apps.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(a => a.app_name.ToLower().Contains(f) || (a.host ?? "").ToLower().Contains(f));
            }

            return await query.OrderBy(a => a.app_name)
                .Select(a => new ApplicationMonitorDto(a.app_name, a.app_type, a.host, a.command_port, a.status))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<InterfaceMonitorDto>> GetInterfacesAsync(string? filter = null)
        {
            var query = _context.Nodes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(n => n.node_name.ToLower().Contains(f) || (n.app_name ?? "").ToLower().Contains(f));
            }

            return await query.OrderBy(n => n.node_id)
                .Select(n => new InterfaceMonitorDto(n.node_id, n.node_name, n.app_name, n.port_in, n.port_out, n.inst_id, n.last_echo, n.status))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ConnectionMonitorDto>> GetConnectionsAsync(string? filter = null)
        {
            var query = _context.Connections.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.conn_name.ToLower().Contains(f) || (c.ip_address ?? "").ToLower().Contains(f));
            }

            return await query.OrderBy(c => c.conn_name)
                .Select(c => new ConnectionMonitorDto(c.conn_name, c.conn_type, c.protocol, c.ip_address, c.port, c.ws_url, c.ws_method, c.ws_content, c.remote))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TerminalMonitorDto>> GetTerminalsAsync(string? filter = null)
        {
            var query = _context.Terminals.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(t => t.term_id.ToLower().Contains(f) || (t.merchant_id ?? "").ToLower().Contains(f));
            }

            return await query.OrderBy(t => t.term_id)
                .Select(t => new TerminalMonitorDto(t.term_id, t.merchant_id, t.location, t.brand, t.type, t.serial_number, t.status))
                .ToListAsync();
        }

        // Mirrors legacy AppGetInterface: "Interface Services" only shows apps flagged
        // app_type == "1" (Interface, per GetAppType) — Core apps (app_type == "0") don't run
        // as a monitorable OS service in the same way.
        public async Task<IReadOnlyList<ServiceMonitorDto>> GetServicesAsync(string? filter = null)
        {
            var query = _context.Apps.AsNoTracking().Where(a => a.app_type == "1");

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(a => a.app_name.ToLower().Contains(f));
            }

            return await query.OrderBy(a => a.app_name)
                .Select(a => new ServiceMonitorDto(a.app_name, a.app_type, a.status, a.last_update))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<JobMonitorDto>> GetJobsAsync(string? filter = null)
        {
            var query = _context.Jobs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(j => (j.job_name ?? "").ToLower().Contains(f));
            }

            return await query.OrderBy(j => j.job_name)
                .Select(j => new JobMonitorDto(j.job_name ?? string.Empty, j.last_running, j.status))
                .ToListAsync();
        }

        // Mirrors legacy JobLogGetRecords: join sw_job_logs to sw_jobs for the display-only
        // job_name, default-sorted newest-first (Desc) rather than the ascending default used
        // by every other Monitoring list — same as legacy's CurrentSortOrder = "Desc" default.
        public async Task<IReadOnlyList<JobLogMonitorDto>> GetJobLogsAsync(string? filter = null)
        {
            var query = from log in _context.JobLogs.AsNoTracking()
                        join job in _context.Jobs.AsNoTracking() on log.job_id equals job.job_id
                        orderby log.job_nr descending
                        select new { log, job.job_name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.job_name ?? "").ToLower().Contains(f));
            }

            var rows = await query.ToListAsync();
            return rows.Select(x => new JobLogMonitorDto(x.log.job_nr, x.job_name, x.log.datetime_begin, x.log.datetime_end, x.log.result_value, x.log.status)).ToList();
        }
    }
}
