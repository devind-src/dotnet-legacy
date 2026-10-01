using Microsoft.Extensions.Options;
using SyncNet.Template.Iso;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Mapping;

/// <summary>CoreRequest → ISO 8583 biller.</summary>
public sealed class ToRemote(IOptions<BillerOptions> options)
{
    public IsoMessage Inquiry(CoreRequest request) => Financial("0200", request);

    public IsoMessage Payment(CoreRequest request) => Financial("0200", request);

    public IsoMessage Advice(CoreRequest request) => Financial("0220", request);

    // Reversal: field 90 berisi data transaksi asal (original_data dari Core = tran type + datetime + trace).
    public IsoMessage Reversal(CoreRequest request) => Financial("0400", request)
        .Set(90, Fit("0200" + request.OriginalData, 42, '0'));

    private IsoMessage Financial(string mti, CoreRequest request)
    {
        // TODO(2): sesuaikan field dengan spesifikasi biller.
        return new IsoMessage(BillerIsoSpec.Instance, mti)
            .Set(2, request.Pan)
            .Set(3, ProcessingCode.FromTranType(request.TranType, request.TranTypeExt, request.FromAccountType, request.ToAccountType))
            .Set(4, Fit(((long)request.Amount).ToString(System.Globalization.CultureInfo.InvariantCulture), 12, '0'))
            .Set(7, Fit(request.TransactionDateTime, 10, '0'))
            .Set(11, Fit(request.TraceNumber, 6, '0'))
            .Set(32, options.Value.AcquirerId)
            .Set(37, Fit(request.ReferenceNumber, 12, ' ', padLeft: false))
            .Set(41, Fit(request.TerminalId, 8, ' ', padLeft: false))
            .Set(42, Fit(request.MerchantId, 15, ' ', padLeft: false))
            .Set(48, request.ToAccountNumber)
            .Set(49, request.Currency ?? "360");
    }

    /// <summary>Pas-kan nilai ke panjang tetap: pad (kiri untuk angka, kanan untuk teks) atau potong.</summary>
    internal static string Fit(string? value, int length, char pad, bool padLeft = true)
    {
        value ??= string.Empty;
        if (value.Length > length) return padLeft ? value[^length..] : value[..length];
        return padLeft ? value.PadLeft(length, pad) : value.PadRight(length, pad);
    }
}
