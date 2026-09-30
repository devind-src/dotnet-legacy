using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetPro.Sdk.Logging;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tracing;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Commands;

/// <summary>
/// Command port (dok. 02 §4): <c>VERSION</c>, <c>RESYNC</c>, <c>ECHO|SIGNON|SIGNOFF|KEYCHANGE &lt;node&gt;</c>,
/// <c>OTHER &lt;node&gt; &lt;param&gt;</c>, <c>TRACE ON|OFF|CLEAR</c>. Pencocokan tidak peka huruf besar/kecil dan
/// <c>TRACE CLEAR</c> benar-benar ditangani (perbaikan B10).
/// </summary>
public sealed class CommandServer : IAsyncDisposable
{
    /// <summary>Balasan sukses.</summary>
    public const string Ok = "OK";

    /// <summary>Balasan perintah tidak dikenal (sama dengan SDK lama).</summary>
    public const string Unknown = "Unknown command";

    private readonly SyncNetOptions _options;
    private readonly SyncNetInterface _handler;
    private readonly INodeRegistry _registry;
    private readonly ITraceWriter _trace;
    private readonly SyncNetServices _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CommandServer> _logger;
    private readonly Func<CancellationToken, Task> _reload;
    private readonly string _version;
    private TcpFrameServer? _server;

    internal CommandServer(IOptions<SyncNetOptions> options, string version, SyncNetInterface handler, INodeRegistry registry,
        SyncNetServices services, ILoggerFactory loggerFactory, Func<CancellationToken, Task> reload)
    {
        _options = options.Value;
        _version = version;
        _handler = handler;
        _registry = registry;
        _trace = services.Trace;
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<CommandServer>();
        _reload = reload;
    }

    /// <summary>Alamat yang didengarkan.</summary>
    public IPEndPoint? LocalEndPoint => _server?.LocalEndPoint;

    /// <summary>Mulai mendengarkan di port.</summary>
    public async Task StartAsync(int port, CancellationToken cancellationToken)
    {
        IPAddress address = IPAddress.Parse(_options.Command.BindAddress);
        _server = new TcpFrameServer(new IPEndPoint(address, port), LengthPrefixCodec.Default, new TcpFrameServerOptions(), _logger)
        {
            FrameReceived = async (connection, payload) =>
            {
                string reply = await ExecuteAsync(Encoding.UTF8.GetString(payload), cancellationToken).ConfigureAwait(false);
                await connection.SendAsync(Encoding.UTF8.GetBytes(reply), cancellationToken).ConfigureAwait(false);
            },
        };
        await _server.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{AppName} command listening at port {Port}", _options.AppName, _server.LocalEndPoint.Port);
    }

    /// <summary>Menjalankan satu perintah teks dan mengembalikan balasannya.</summary>
    public async Task<string> ExecuteAsync(string text, CancellationToken cancellationToken)
    {
        string input = (text ?? string.Empty).Trim();
        _logger.LogInformation("Receive command: {Command}", input);

        string[] tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return Unknown;

        string first = tokens[0].ToUpperInvariant();
        string rest = tokens.Length > 1 ? string.Join(' ', tokens[1..]) : string.Empty;

        try
        {
            switch (first)
            {
                case "VERSION":
                    return _version;
                case "RESYNC":
                    await _reload(cancellationToken).ConfigureAwait(false);
                    return Ok;
                case "ECHO":
                    return await NetworkAsync(NetworkCommand.Echo, rest, string.Empty, cancellationToken).ConfigureAwait(false);
                case "SIGNON":
                    return await NetworkAsync(NetworkCommand.SignOn, rest, string.Empty, cancellationToken).ConfigureAwait(false);
                case "SIGNOFF":
                    return await NetworkAsync(NetworkCommand.SignOff, rest, string.Empty, cancellationToken).ConfigureAwait(false);
                case "KEYCHANGE":
                    return await NetworkAsync(NetworkCommand.KeyChange, rest, string.Empty, cancellationToken).ConfigureAwait(false);
                case "OTHER":
                    string node = tokens.Length > 1 ? tokens[1] : string.Empty;
                    string parameter = tokens.Length > 2 ? string.Join(' ', tokens[2..]) : string.Empty;
                    return await NetworkAsync(NetworkCommand.Other, node, parameter, cancellationToken).ConfigureAwait(false);
                case "TRACE" when tokens.Length == 2:
                    switch (tokens[1].ToUpperInvariant())
                    {
                        case "ON":
                            _trace.SetEnabled(true);
                            return Ok;
                        case "OFF":
                            _trace.SetEnabled(false);
                            return Ok;
                        case "CLEAR":
                            int cleared = _trace.Clear();
                            _logger.LogInformation("{Count} trace dalam antrean dibuang", cleared);
                            return Ok;
                    }

                    return Unknown;
                default:
                    return Unknown;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Perintah {Command} gagal", input);
            return $"ERROR {ex.Message}";
        }
    }

    private async Task<string> NetworkAsync(NetworkCommand command, string nodeName, string parameter, CancellationToken cancellationToken)
    {
        _registry.TryGetNode(nodeName, out NodeInfo? node);
        ILogger logger = _loggerFactory.CreateLogger(_handler.GetType());
        using IDisposable? scope = logger.BeginNodeScope(nodeName);
        var context = new NetworkCommandContext(command, nodeName, parameter, node, _services, logger);
        await _handler.OnNetworkCommandAsync(context, cancellationToken).ConfigureAwait(false);
        return Ok;
    }

    /// <summary>Berhenti mendengarkan.</summary>
    public async Task StopAsync()
    {
        if (_server is not null) await _server.StopAsync().ConfigureAwait(false);
        _server = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
