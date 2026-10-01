using System.Globalization;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Iso;

/// <summary>Pesan network management (0800).</summary>
public static class NetworkMessages
{
    private static int _stan = Random.Shared.Next(1, 999_999);

    public const string SignOnCode = "001";
    public const string EchoCode = "301";

    public static IsoMessage SignOn(string acquirerId) => Create(SignOnCode, acquirerId);

    public static IsoMessage Echo(string acquirerId) => Create(EchoCode, acquirerId);

    private static IsoMessage Create(string code, string acquirerId) => new IsoMessage(BillerIsoSpec.Instance, "0800")
        .Set(7, DateTime.UtcNow.ToString("MMddHHmmss", CultureInfo.InvariantCulture))
        .Set(11, (Interlocked.Increment(ref _stan) % 1_000_000).ToString("D6", CultureInfo.InvariantCulture))
        .Set(32, acquirerId)
        .Set(70, code);
}
