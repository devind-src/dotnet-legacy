namespace SyncNetPro.Contracts;

/// <summary>Nilai <c>authorized_by</c> pada <see cref="CoreResponse"/>.</summary>
public static class AuthorizedBy
{
    /// <summary>Respons diputuskan interface/SyncNet sendiri (mis. transaksi tidak didukung, link down).</summary>
    public const string Internal = "0";

    /// <summary>Respons berasal dari sistem eksternal (default).</summary>
    public const string External = "1";
}
