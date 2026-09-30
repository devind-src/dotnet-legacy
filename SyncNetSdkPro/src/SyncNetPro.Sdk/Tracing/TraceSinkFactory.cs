using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetPro.Sdk.Configuration;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>Memilih sink trace sesuai opsi, config Core, dan <c>sw_app</c>.</summary>
public sealed class TraceSinkFactory(
    IOptions<SyncNetOptions> options,
    CoreEnvironment environment,
    INodeRegistry registry,
    TimeProvider time,
    ILoggerFactory loggerFactory)
{
    /// <summary>Membuat sink utama dan fallback (file).</summary>
    public (ITraceSink Primary, ITraceSink Fallback) Create()
    {
        TraceOptions o = options.Value.Trace;
        var file = new FileTraceSink(environment.TraceDirectory, options.Value.Logging.LegacyWindowsFileNames);
        ILogger logger = loggerFactory.CreateLogger("SyncNetPro.Sdk.Tracing");

        string? host = o.LogServicesHost ?? registry.LogServices?.Host;
        int? port = o.LogServicesPort ?? registry.LogServices?.Port;

        ITraceSink? primary = o.Sink switch
        {
            TraceSinkKind.File or TraceSinkKind.None => file,
            TraceSinkKind.RabbitMq when environment.RabbitMq is { } rabbit => new RabbitMqTraceSink(rabbit, logger, time),
            TraceSinkKind.RabbitMq => throw new InvalidOperationException("Trace:Sink=RabbitMq tetapi RabbitMQ tidak diaktifkan di config Core."),
            TraceSinkKind.LogServices when host is not null && port is > 0 => new LogServicesTcpSink(host, port.Value, logger, time),
            TraceSinkKind.LogServices => throw new InvalidOperationException("Trace:Sink=LogServices tetapi alamat Log Services tidak diketahui (sw_app 'Log Services' / SyncNet:Trace:LogServicesHost/Port)."),
            _ when environment.RabbitMq is { } rabbit => new RabbitMqTraceSink(rabbit, logger, time),
            _ when host is not null && port is > 0 => new LogServicesTcpSink(host, port.Value, logger, time),
            _ => null,
        };

        return (primary ?? file, file);
    }
}
