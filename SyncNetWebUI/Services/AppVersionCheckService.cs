using System.Text.Json;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Polls the app's own wwwroot/version.json (restamped on every build — see the
    /// WriteAppVersionFile target in SyncNetWasm.csproj) to detect a new deploy while a tab is
    /// still open. Framework/app files are fingerprinted so a plain page refresh always picks up
    /// the right build; this only covers tabs that are never reloaded on their own.
    /// </summary>
    public class AppVersionCheckService : IAsyncDisposable
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(10);

        private readonly HttpClient _http;
        private string? _knownVersion;
        private CancellationTokenSource? _loopCts;

        public event Action? UpdateAvailable;

        public AppVersionCheckService(HttpClient http) => _http = http;

        public async Task StartAsync()
        {
            if (_loopCts != null) return;

            _knownVersion = await FetchVersionAsync(CancellationToken.None);

            _loopCts = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCts.Token);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(PollInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    var latest = await FetchVersionAsync(ct);
                    if (latest != null && _knownVersion != null && latest != _knownVersion)
                    {
                        UpdateAvailable?.Invoke();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose.
            }
        }

        private async Task<string?> FetchVersionAsync(CancellationToken ct)
        {
            try
            {
                // version.json is already served no-cache (see web.config), but the query string
                // also defeats any intermediary that ignores that header.
                using var response = await _http.GetAsync($"version.json?t={DateTime.UtcNow.Ticks}", ct);
                if (!response.IsSuccessStatusCode) return null;

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return doc.RootElement.GetProperty("version").GetString();
            }
            catch
            {
                // Connectivity hiccup — retry on the next tick instead of surfacing an error.
                return null;
            }
        }

        public ValueTask DisposeAsync()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
            return ValueTask.CompletedTask;
        }
    }
}
