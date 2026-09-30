using System.Text;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Testing;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.SimCore.Tests;

/// <summary>Interface contoh: INQ disetujui (+trace), PAY diteruskan ke stub TCP, lainnya default.</summary>
public sealed class BillerInterface : SyncNetInterface
{
    public TimeSpan Delay { get; set; }

    public int Requests;

    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Requests);
        if (Delay > TimeSpan.Zero) await Task.Delay(Delay, cancellationToken);

        switch (context.Request.TranType)
        {
            case TranType.Inquiry:
                context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, "INQ", "detail", "stub");
                return context.Request.ToResponse("00", "Approved").SetAdditionalData("customer_name", "BUDI");
            case TranType.Payment:
                byte[] reply = await context.Remote.SendAndReceiveAsync(Encoding.UTF8.GetBytes($"PAY|{context.Request.TraceNumber}"), context.Request.TraceNumber!, cancellationToken: cancellationToken);
                return context.Request.ToResponse(Encoding.UTF8.GetString(reply).Split('|')[2]);
            case TranType.Reversal:
                return context.Request.ToResponse("00", "Reversed");
            default:
                return await base.OnCoreRequestAsync(context, cancellationToken);
        }
    }

    public override string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message)
    {
        string[] parts = Encoding.UTF8.GetString(message).Split('|');
        return parts.Length >= 2 ? parts[1] : null;
    }
}

/// <summary>Interface channel: meneruskan ke Core (kanal source).</summary>
public sealed class ChannelInterface : SyncNetInterface;

internal static class Samples
{
    public static CoreRequest Request(string tranType = TranType.Inquiry, string trace = "000123") => new()
    {
        MessageType = "0200",
        TranType = tranType,
        TraceNumber = trace,
        TransactionDateTime = "0930101530",
        TerminalId = "TERM0001",
        Amount = 150000m,
        PosEntryMode = "021",
    };

    public static SimCoreOptions Options(NodeCategory category = NodeCategory.BillerIssuer, int timeoutSeconds = 5) => new()
    {
        AppName = "API Test",
        LogServicesPort = 0,
        Nodes = [new SimNodeOptions { Name = "BILLER", Category = category, RequestTimeoutSeconds = timeoutSeconds, AdviceTimeoutSeconds = timeoutSeconds }],
    };
}
