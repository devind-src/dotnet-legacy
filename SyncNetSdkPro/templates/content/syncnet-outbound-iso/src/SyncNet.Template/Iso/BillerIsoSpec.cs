using SyncNetPro.Iso8583;

namespace SyncNet.Template.Iso;

/// <summary>
/// Spesifikasi ISO 8583 biller. Dimulai dari tabel default SyncNet (<see cref="IsoSpec.Legacy"/>) lalu field yang
/// berbeda ditimpa — sama seperti <c>IsoTemplate : FieldFormatter</c> di interface lama.
/// </summary>
public static class BillerIsoSpec
{
    // TODO(1): sesuaikan dengan dokumen spesifikasi biller (panjang, tipe, encoding BCD/EBCDIC, TPDU).
    public static IsoSpec Instance { get; } = IsoSpec.Legacy.ToBuilder()
        .Field(7, IsoLengthType.Fixed, IsoFieldContent.N, 10, "Transmission Date Time")
        .Field(11, IsoLengthType.Fixed, IsoFieldContent.N, 6, "System Trace Audit Number")
        .Field(37, IsoLengthType.Fixed, IsoFieldContent.Ans, 12, "Retrieval Reference Number")
        .Field(39, IsoLengthType.Fixed, IsoFieldContent.Ans, 2, "Response Code")
        .Field(41, IsoLengthType.Fixed, IsoFieldContent.Ans, 8, "Card Acceptor Terminal ID")
        .Field(42, IsoLengthType.Fixed, IsoFieldContent.Ans, 15, "Card Acceptor ID Code")
        .Field(49, IsoLengthType.Fixed, IsoFieldContent.Ans, 3, "Currency Code")
        .Build();

    /// <summary>Kunci korelasi request ↔ response: STAN + terminal (sama di request dan response biller).</summary>
    public static string CorrelationKey(IsoMessage message) => $"{message[11]}|{message[41]}";
}
