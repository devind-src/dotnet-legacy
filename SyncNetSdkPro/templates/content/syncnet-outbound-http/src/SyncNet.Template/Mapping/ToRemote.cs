using Microsoft.Extensions.Options;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Mapping;

/// <summary>CoreRequest → request HTTP biller (path + body).</summary>
public sealed class ToRemote(IOptions<BillerOptions> options)
{
    // TODO(2): path endpoint biller per transaksi.
    public (string Path, BillerRequest Body)? Map(CoreRequest request) => request.TranType switch
    {
        TranType.Inquiry => ("/bill/inquiry", Body(request)),
        TranType.Payment => ("/bill/payment", Body(request)),
        TranType.Advice => ("/bill/advice", Body(request)),
        TranType.Reversal => ("/bill/reversal", Body(request)),
        _ => null,
    };

    private BillerRequest Body(CoreRequest request) => new()
    {
        PartnerId = options.Value.PartnerId,
        Reference = request.ReferenceNumber,
        TraceNumber = request.TraceNumber,
        TerminalId = request.TerminalId,
        ProductCode = request.TranTypeExt,
        CustomerId = request.ToAccountNumber,
        Amount = request.Amount,
        OriginalReference = request.OriginalData,
    };

    /// <summary>Header request (API key, dsb.).</summary>
    public IReadOnlyDictionary<string, string> Headers(string? connectionKey)
    {
        var headers = new Dictionary<string, string>(options.Value.Headers);
        if (headers.Count == 0 && !string.IsNullOrEmpty(connectionKey)) headers["X-Api-Key"] = connectionKey;
        return headers;
    }
}
