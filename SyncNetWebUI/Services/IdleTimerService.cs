using Microsoft.JSInterop;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Tracks user inactivity and drives the "you're about to be logged out" warning +
    /// automatic logout. All timing lives here in C# — the JS side (wwwroot/js/idle-activity.js)
    /// only pings OnActivity() on DOM events, throttled. Wired up by MainLayout, which only
    /// mounts for authenticated routes, so this is naturally inactive on the login page.
    /// </summary>
    public class IdleTimerService : IAsyncDisposable
    {
        // Internal admin tool, not a public/customer-facing app (see PROJECT_TECHNICAL_SUMMARY
        // §8) — a 15-minute idle window with a 1-minute warning is a reasonable default for
        // that usage pattern without being intrusive during normal work.
        public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan WarningBeforeTimeout = TimeSpan.FromMinutes(1);

        private readonly IJSRuntime _js;
        private DotNetObjectReference<IdleTimerService>? _selfRef;
        private CancellationTokenSource? _loopCts;
        private DateTime _lastActivityUtc;
        private bool _warningShown;
        private bool _started;

        public event Action? WarningShow;
        public event Action<int>? WarningTick;
        public event Func<Task>? TimedOut;

        public int SecondsRemaining =>
            Math.Max(0, (int)Math.Ceiling((IdleTimeout - (DateTime.UtcNow - _lastActivityUtc)).TotalSeconds));

        public IdleTimerService(IJSRuntime js) => _js = js;

        public async Task StartAsync()
        {
            if (_started) return;
            _started = true;

            _lastActivityUtc = DateTime.UtcNow;
            _warningShown = false;

            _selfRef = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("startIdleActivityListener", _selfRef);

            _loopCts = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCts.Token);
        }

        public async Task StopAsync()
        {
            if (!_started) return;
            _started = false;

            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;

            if (_selfRef != null)
            {
                try
                {
                    await _js.InvokeVoidAsync("stopIdleActivityListener");
                }
                catch (JSDisconnectedException)
                {
                    // Circuit/page already gone (e.g. tab closed) — nothing left to clean up.
                }

                _selfRef.Dispose();
                _selfRef = null;
            }
        }

        /// <summary>Called from JS on throttled DOM activity, and directly by the warning
        /// dialog's "Tetap Login" button.</summary>
        [JSInvokable]
        public void OnActivity()
        {
            _lastActivityUtc = DateTime.UtcNow;
            _warningShown = false;
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    var idleFor = DateTime.UtcNow - _lastActivityUtc;

                    if (idleFor >= IdleTimeout)
                    {
                        if (TimedOut != null) await TimedOut.Invoke();
                        return;
                    }

                    if (idleFor >= IdleTimeout - WarningBeforeTimeout)
                    {
                        if (!_warningShown)
                        {
                            _warningShown = true;
                            WarningShow?.Invoke();
                        }

                        WarningTick?.Invoke(SecondsRemaining);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on StopAsync/dispose.
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync();
    }
}
