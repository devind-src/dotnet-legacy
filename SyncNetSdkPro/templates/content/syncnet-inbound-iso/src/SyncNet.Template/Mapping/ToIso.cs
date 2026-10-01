using System.Globalization;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Mapping;

/// <summary>CoreResponse → ISO 8583 response ke pengirim.</summary>
public sealed class ToIso
{
    // TODO(4): field yang dikembalikan ke pengirim.
    public IsoMessage From(IsoMessage request, CoreResponse response)
    {
        IsoMessage reply = request.CreateResponse(Fit(response.ResponseCode ?? "06"));
        reply.Set(4, ((long)response.Amount).ToString("D12", CultureInfo.InvariantCulture));
        if (response.AdditionalData?.GetValueOrDefault("approval_code") is { } approval) reply.Set(38, Convert.ToString(approval, CultureInfo.InvariantCulture));
        if (response.AdditionalData?.GetValueOrDefault("bill_info") is { } billInfo) reply.Set(48, Convert.ToString(billInfo, CultureInfo.InvariantCulture));
        return reply;
    }

    /// <summary>Response tanpa Core (timeout, Core tidak tersedia, transaksi tidak didukung).</summary>
    public static IsoMessage Status(IsoMessage request, string responseCode) => request.CreateResponse(Fit(responseCode));

    // Field 39 dua karakter.
    private static string Fit(string rc) => rc.Length >= 2 ? rc[..2] : rc.PadLeft(2, '0');
}
