namespace SyncNetApi.Services.Monitoring
{
    /// <summary>Raw TCP command channel to a live switch process (Application command port or
    /// Node's owning Application) — ports legacy `NbCommand`/`XSocketClient`/`NbTcpHeader`
    /// (SyncNetBlazorServer/.../Library, /Networking) to modern .NET. Talks to whatever
    /// host/port the caller resolves (host, port), so this class itself has no notion of
    /// "which app/node" — that's MonitoringCommandService's job (including validating the
    /// command against an allow-list before ever getting here).</summary>
    public interface ISwitchCommandClient
    {
        /// <summary>Sends <paramref name="command"/> framed per the legacy TCP protocol and
        /// waits up to ~3 seconds for a response. Returns an empty string on ANY failure
        /// (connect refused/timeout, no response, connection dropped mid-read) — mirrors
        /// legacy NbCommand.Send, which never throws and just leaves its response empty in
        /// every one of those cases. This is a monitoring probe against processes that may
        /// legitimately be down; a 500 here would be misleading.</summary>
        Task<string> SendAsync(string host, int port, string command, CancellationToken ct = default);
    }
}
