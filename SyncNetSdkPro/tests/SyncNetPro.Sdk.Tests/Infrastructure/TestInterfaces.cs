using System.Collections.Concurrent;
using SyncNetPro.Contracts;
using System.Text;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;

namespace SyncNetPro.Sdk.Tests.Infrastructure;

/// <summary>Interface tanpa override: memakai semua perilaku default SDK.</summary>
public sealed class DefaultInterface : SyncNetInterface;

/// <summary>Interface yang perilakunya diatur test.</summary>
public sealed class ScriptedInterface : SyncNetInterface
{
    public Func<CoreRequestContext, Task<CoreResponse?>>? OnRequest { get; set; }

    public ConcurrentQueue<NetworkCommandContext> Commands { get; } = new();

    public ConcurrentQueue<CoreResponse> Unmatched { get; } = new();

    public int Reloads;

    public int Started;

    public int Stopping;

    public Func<RemoteMessageContext, Task>? OnRemote { get; set; }

    public Func<HttpRequestContext, Task<HttpReply>>? OnHttp { get; set; }

    public Func<RemoteConnectionContext, Task>? OnSignOn { get; set; }

    /// <summary>Kunci korelasi: teks sebelum '|' (format pesan test "KEY|isi").</summary>
    public bool UsePipeCorrelation { get; set; }

    public ConcurrentQueue<string> Events { get; } = new();

    public int Echoes;

    public int KeyExchanges;

    public override Task OnRemoteMessageAsync(RemoteMessageContext context, CancellationToken cancellationToken)
    {
        Events.Enqueue("message:" + Encoding.UTF8.GetString(context.Message));
        return OnRemote?.Invoke(context) ?? Task.CompletedTask;
    }

    public override Task<HttpReply> OnHttpRequestAsync(HttpRequestContext context, CancellationToken cancellationToken) =>
        OnHttp is null ? base.OnHttpRequestAsync(context, cancellationToken) : OnHttp(context);

    public override string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message)
    {
        if (!UsePipeCorrelation) return null;
        string text = Encoding.UTF8.GetString(message);
        int pipe = text.IndexOf('|', StringComparison.Ordinal);
        return pipe > 0 ? text[..pipe] : null;
    }

    public override Task OnRemoteConnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken)
    {
        Events.Enqueue("connected:" + context.Connection.Info.Name);
        return Task.CompletedTask;
    }

    public override Task OnRemoteDisconnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken)
    {
        Events.Enqueue("disconnected:" + context.Connection.Info.Name);
        return Task.CompletedTask;
    }

    public override Task OnAutoSignOnAsync(RemoteConnectionContext context, CancellationToken cancellationToken)
    {
        Events.Enqueue("signon:" + context.Connection.Info.Name);
        return OnSignOn?.Invoke(context) ?? Task.CompletedTask;
    }

    public override Task OnEchoTimerAsync(NodeContext context, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Echoes);
        return Task.CompletedTask;
    }

    public override Task OnKeyExchangeTimerAsync(NodeContext context, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref KeyExchanges);
        return Task.CompletedTask;
    }

    public override Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken) =>
        OnRequest is null ? base.OnCoreRequestAsync(context, cancellationToken) : OnRequest(context);

    public override Task OnNetworkCommandAsync(NetworkCommandContext context, CancellationToken cancellationToken)
    {
        Commands.Enqueue(context);
        return Task.CompletedTask;
    }

    public override Task OnUnmatchedCoreResponseAsync(CoreResponseContext context, CancellationToken cancellationToken)
    {
        Unmatched.Enqueue(context.Response);
        return Task.CompletedTask;
    }

    public override Task OnConfigurationReloadedAsync(NodeConfiguration configuration, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Reloads);
        return Task.CompletedTask;
    }

    public override Task OnStartedAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Started);
        return Task.CompletedTask;
    }

    public override Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Stopping);
        return Task.CompletedTask;
    }
}

/// <summary>Sumber konfigurasi yang bisa diubah test (untuk RESYNC).</summary>
public sealed class MutableNodeSource : INodeConfigurationSource
{
    public List<NodeInfo> Nodes { get; set; } = [];

    public List<RemoteConnectionInfo> Connections { get; set; } = [];

    public int? CommandPort { get; set; }

    public System.Net.DnsEndPoint? LogServices { get; set; }

    public Task<NodeConfiguration> LoadAsync(string appName, CancellationToken cancellationToken) =>
        Task.FromResult(new NodeConfiguration([.. Nodes], [.. Connections], CommandPort, LogServices));
}

internal static class Messages
{
    public static CoreRequest Request(string trace = "000123", string tranType = TranType.Inquiry, string terminal = "TERM0001") => new()
    {
        MessageType = "0200",
        TranType = tranType,
        TraceNumber = trace,
        TransactionDateTime = "0930101530",
        TerminalId = terminal,
        MerchantId = "MERCHANT01",
        PosEntryMode = "021",
        Amount = 150000m,
    };

    public static NodeInfo Node(string name, NodeCategory category, FakeCore core, int timeoutSeconds = 30) => new()
    {
        Name = name,
        Category = category,
        PortIn = core.SourcePort,
        PortOut = core.SinkPort,
        RequestTimeoutSeconds = timeoutSeconds,
        AdviceTimeoutSeconds = timeoutSeconds,
    };
}

/// <summary>Pelapor status yang merekam panggilan.</summary>
public sealed class RecordingStatusReporter : INodeStatusReporter
{
    public ConcurrentQueue<string> Calls { get; } = new();

    public Task ReportApplicationAsync(string appName, bool up, CancellationToken cancellationToken = default) => Record($"app:{appName}:{up}");

    public Task ReportCoreChannelAsync(string nodeName, CoreChannelDirection direction, bool up, CancellationToken cancellationToken = default) =>
        Record($"core:{nodeName}:{direction}:{up}");

    public Task ReportRemoteNodeAsync(string nodeName, bool up, CancellationToken cancellationToken = default) => Record($"remote:{nodeName}:{up}");

    public Task ReportConnectionAsync(string connectionName, bool up, CancellationToken cancellationToken = default) => Record($"conn:{connectionName}:{up}");

    public bool Has(string call) => Calls.Contains(call);

    private Task Record(string call)
    {
        Calls.Enqueue(call);
        return Task.CompletedTask;
    }
}
