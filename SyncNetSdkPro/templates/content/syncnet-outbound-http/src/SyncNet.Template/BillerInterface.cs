using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SyncNet.Template.Mapping;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;

namespace SyncNet.Template;

/// <summary>
/// Interface outbound: menerima request dari Core (kanal sink / port_out), meneruskan ke biller via HTTP/JSON,
/// lalu membalas Core. Base URL = <c>sw_connections.ws_url</c> node.
/// </summary>
public sealed class BillerInterface(ToRemote toRemote, ToCore toCore) : SyncNetInterface
{
    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        CoreRequest request = context.Request;

        // TODO(4): tran_type yang didukung ditentukan di ToRemote.Map.
        if (toRemote.Map(request) is not var (path, body)) return request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal);

        IRemoteHttpClient http = context.Remote.Http;
        RemoteHttpRequest httpRequest = RemoteHttpRequest.Json(path, body, toRemote.Headers(http.Info.WsKey));
        context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, "REQ " + path, httpRequest.Body ?? string.Empty, http.Info.WsUrl);

        RemoteHttpResponse response;
        try
        {
            response = await http.SendAsync(httpRequest, cancellationToken);
        }
        catch (RemoteUnavailableException ex)
        {
            context.Logger.LogWarning("Biller tidak dapat dihubungi: {Error}", ex.Message);
            return request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal);
        }

        context.Trace.Message(context.Node.Name, TraceDirection.Incoming, $"RSP {(int)response.StatusCode}", response.Body, http.Info.WsUrl);
        if (!response.IsSuccess) return toCore.SystemError(request, $"HTTP {(int)response.StatusCode}");

        try
        {
            BillerResponse? reply = response.ReadJson<BillerResponse>();
            return reply is null ? toCore.SystemError(request, "Empty response") : toCore.From(reply, request);
        }
        catch (JsonException)
        {
            return toCore.SystemError(request, "Invalid response");
        }
    }
}
