namespace SyncNetPro.Sdk;

/// <summary>Kode respons yang lazim dipakai interface saat memutuskan respons sendiri.</summary>
public static class ResponseCodes
{
    /// <summary>Disetujui.</summary>
    public const string Approved = "00";

    /// <summary>Transaksi tidak didukung interface (konvensi interface lama).</summary>
    public const string NotSupported = "A1";

    /// <summary>Link ke sistem eksternal terputus (konvensi interface lama).</summary>
    public const string LinkDown = "89";
}
