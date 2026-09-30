using System.Collections.Concurrent;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;

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

    public int? CommandPort { get; set; }

    public System.Net.DnsEndPoint? LogServices { get; set; }

    public Task<NodeConfiguration> LoadAsync(string appName, CancellationToken cancellationToken) =>
        Task.FromResult(new NodeConfiguration([.. Nodes], [], CommandPort, LogServices));
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
