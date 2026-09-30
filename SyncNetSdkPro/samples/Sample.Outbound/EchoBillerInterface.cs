using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Tracing;

namespace Sample.Outbound;

/// <summary>
/// Contoh interface outbound paling sederhana: menyetujui INQ/PAY dari Core tanpa sistem eksternal.
/// Transport ke biller (TCP/HTTP) ditambahkan pada fase 3.
/// </summary>
public sealed class EchoBillerInterface(ILogger<EchoBillerInterface> logger) : SyncNetInterface
{
    public override Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        CoreRequest request = context.Request;
        context.Trace.Message(context.Node.Name, TraceDirection.Incoming, request.MessageType ?? "REQ", $"trace {request.TraceNumber}", "core");

        CoreResponse? response = request.TranType switch
        {
            TranType.Inquiry => request.ToResponse(ResponseCodes.Approved, "Approved")
                .SetAdditionalData("customer_name", "PELANGGAN CONTOH"),
            TranType.Payment => request.ToResponse(ResponseCodes.Approved, "Approved"),
            _ => null,
        };

        if (response is null) return base.OnCoreRequestAsync(context, cancellationToken);

        logger.LogInformation("{TranType} trace {Trace} disetujui", request.TranType, request.TraceNumber);
        return Task.FromResult<CoreResponse?>(response);
    }
}
