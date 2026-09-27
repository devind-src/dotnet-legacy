using System.Net.Http.Json;
using SyncNetApi.Dtos.Monitoring;

namespace SyncNetWasm.Services
{
    public class MonitoringManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MonitoringManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public Task<List<ApplicationMonitorDto>?> GetApplicationsAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<ApplicationMonitorDto>("api/v1/monitoring/applications", filter, ct);

        public Task<List<InterfaceMonitorDto>?> GetInterfacesAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<InterfaceMonitorDto>("api/v1/monitoring/interfaces", filter, ct);

        public Task<List<ConnectionMonitorDto>?> GetConnectionsAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<ConnectionMonitorDto>("api/v1/monitoring/connections", filter, ct);

        public Task<List<TerminalMonitorDto>?> GetTerminalsAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<TerminalMonitorDto>("api/v1/monitoring/terminals", filter, ct);

        public Task<List<ServiceMonitorDto>?> GetServicesAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<ServiceMonitorDto>("api/v1/monitoring/services", filter, ct);

        public Task<List<JobMonitorDto>?> GetJobsAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<JobMonitorDto>("api/v1/monitoring/jobs", filter, ct);

        public Task<List<JobLogMonitorDto>?> GetJobLogsAsync(string? filter = null, CancellationToken ct = default)
            => GetAsync<JobLogMonitorDto>("api/v1/monitoring/job-logs", filter, ct);

        // ---------------------------------------------------------------------------
        // Send Command — Application / Interface
        // ---------------------------------------------------------------------------

        public async Task<(bool Success, string? Response, string? Error)> SendApplicationCommandAsync(string appName, string command, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync(
                $"api/v1/monitoring/applications/{Uri.EscapeDataString(appName)}/command",
                new SendApplicationCommandRequest { Command = command }, ct);

            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var dto = await response.Content.ReadFromJsonAsync<CommandResponseDto>(cancellationToken: ct);
            return (true, dto?.Response, null);
        }

        public async Task<(bool Success, string? Response, string? Error)> SendInterfaceCommandAsync(int nodeId, string command, string? otherCommand, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync(
                $"api/v1/monitoring/interfaces/{nodeId}/command",
                new SendNodeCommandRequest { Command = command, OtherCommand = otherCommand }, ct);

            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var dto = await response.Content.ReadFromJsonAsync<CommandResponseDto>(cancellationToken: ct);
            return (true, dto?.Response, null);
        }

        // ---------------------------------------------------------------------------
        // Loggers — Log Viewer
        // ---------------------------------------------------------------------------

        public async Task<List<string>?> GetLogAppsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/monitoring/logs/apps", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct) : null;
        }

        public async Task<List<MonitoringFileDto>?> GetLogFilesAsync(string app, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/monitoring/logs/files?app={Uri.EscapeDataString(app)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MonitoringFileDto>>(cancellationToken: ct) : null;
        }

        public async Task<MonitoringFileContentDto?> GetLogContentAsync(string file, int offset, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/monitoring/logs/content?file={Uri.EscapeDataString(file)}&offset={offset}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MonitoringFileContentDto>(cancellationToken: ct) : null;
        }

        // ---------------------------------------------------------------------------
        // Loggers — Trace Viewer
        // ---------------------------------------------------------------------------

        public async Task<List<string>?> GetTraceAppsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/monitoring/traces/apps", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct) : null;
        }

        public async Task<List<MonitoringFileDto>?> GetTraceFilesAsync(string app, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/monitoring/traces/files?app={Uri.EscapeDataString(app)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MonitoringFileDto>>(cancellationToken: ct) : null;
        }

        public async Task<MonitoringFileContentDto?> GetTraceContentAsync(string app, string file, int offset, string? search = null, CancellationToken ct = default)
        {
            var url = $"api/v1/monitoring/traces/content?app={Uri.EscapeDataString(app)}&file={Uri.EscapeDataString(file)}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MonitoringFileContentDto>(cancellationToken: ct) : null;
        }

        public async Task<(byte[]? Content, string? FileName)> DownloadTraceFileAsync(string app, string file, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/monitoring/traces/download?app={Uri.EscapeDataString(app)}&file={Uri.EscapeDataString(file)}", ct);
            if (!response.IsSuccessStatusCode) return (null, null);

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return (bytes, response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName ?? $"{file}.zip");
        }

        private async Task<List<T>?> GetAsync<T>(string path, string? filter, CancellationToken ct)
        {
            var url = path + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct) : null;
        }
    }
}
