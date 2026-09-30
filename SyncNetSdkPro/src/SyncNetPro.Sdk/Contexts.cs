using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk;

/// <summary>Layanan bersama yang tersedia di setiap konteks handler.</summary>
public abstract class SyncNetContext
{
    private protected SyncNetContext(NodeInfo? node, ICoreClient core, ITraceWriter trace, ILogger logger, IServiceProvider services)
    {
        Node = node;
        Core = core;
        Trace = trace;
        Logger = logger;
        Services = services;
    }

    /// <summary>Node terkait (bisa <c>null</c> untuk command dengan nama node yang tidak dikenal).</summary>
    public NodeInfo? Node { get; }

    /// <summary>Klien ke Core.</summary>
    public ICoreClient Core { get; }

    /// <summary>Trace ke Log Services.</summary>
    public ITraceWriter Trace { get; }

    /// <summary>Logger (sudah dalam scope node).</summary>
    public ILogger Logger { get; }

    /// <summary>Service provider untuk kebutuhan lanjutan.</summary>
    public IServiceProvider Services { get; }
}

/// <summary>Request dari Core pada kanal outbound (Core SinkNode, <c>port_out</c>).</summary>
public sealed class CoreRequestContext : SyncNetContext
{
    private readonly Func<CoreResponse, CancellationToken, Task> _reply;
    private int _replied;

    internal CoreRequestContext(NodeInfo node, CoreRequest request, Func<CoreResponse, CancellationToken, Task> reply,
        ICoreClient core, ITraceWriter trace, ILogger logger, IServiceProvider services)
        : base(node, core, trace, logger, services)
    {
        Request = request;
        _reply = reply;
    }

    /// <summary>Node penerima request.</summary>
    public new NodeInfo Node => base.Node!;

    /// <summary>Request dari Core.</summary>
    public CoreRequest Request { get; }

    /// <summary><c>true</c> bila balasan sudah dikirim.</summary>
    public bool HasReplied => Volatile.Read(ref _replied) != 0;

    /// <summary>
    /// Mengirim balasan ke Core. Cukup kembalikan response dari <see cref="SyncNetInterface.OnCoreRequestAsync"/>;
    /// method ini untuk membalas lebih awal/asinkron. Hanya balasan pertama yang dikirim.
    /// </summary>
    public async Task ReplyAsync(CoreResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (Interlocked.Exchange(ref _replied, 1) != 0)
        {
            Logger.LogWarning("Balasan ganda untuk trace {Trace} diabaikan", Request.TraceNumber);
            return;
        }

        await _reply(response, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Respons Core yang tidak cocok dengan request yang sedang menunggu (terlambat/tidak dikenal).</summary>
public sealed class CoreResponseContext : SyncNetContext
{
    internal CoreResponseContext(NodeInfo node, CoreResponse response, ICoreClient core, ITraceWriter trace, ILogger logger, IServiceProvider services)
        : base(node, core, trace, logger, services)
    {
        Response = response;
    }

    /// <summary>Node.</summary>
    public new NodeInfo Node => base.Node!;

    /// <summary>Respons dari Core.</summary>
    public CoreResponse Response { get; }
}

/// <summary>Perintah manajemen jaringan dari command port.</summary>
public enum NetworkCommand
{
    /// <summary><c>ECHO</c>.</summary>
    Echo,

    /// <summary><c>SIGNON</c>.</summary>
    SignOn,

    /// <summary><c>SIGNOFF</c>.</summary>
    SignOff,

    /// <summary><c>KEYCHANGE</c>.</summary>
    KeyChange,

    /// <summary><c>OTHER &lt;node&gt; &lt;param&gt;</c>.</summary>
    Other,
}

/// <summary>Perintah manajemen jaringan untuk sebuah node.</summary>
public sealed class NetworkCommandContext : SyncNetContext
{
    internal NetworkCommandContext(NetworkCommand command, string nodeName, string parameter, NodeInfo? node,
        ICoreClient core, ITraceWriter trace, ILogger logger, IServiceProvider services)
        : base(node, core, trace, logger, services)
    {
        Command = command;
        NodeName = nodeName;
        Parameter = parameter;
    }

    /// <summary>Perintah.</summary>
    public NetworkCommand Command { get; }

    /// <summary>Nama node sesuai perintah.</summary>
    public string NodeName { get; }

    /// <summary>Parameter (untuk <see cref="NetworkCommand.Other"/>).</summary>
    public string Parameter { get; }
}
