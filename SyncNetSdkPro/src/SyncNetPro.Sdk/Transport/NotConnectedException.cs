namespace SyncNetPro.Sdk.Transport;

/// <summary>Pengiriman gagal karena koneksi tidak tersedia.</summary>
public sealed class NotConnectedException : IOException
{
    /// <summary>Membuat exception.</summary>
    public NotConnectedException()
    {
    }

    /// <summary>Membuat exception dengan pesan.</summary>
    public NotConnectedException(string message)
        : base(message)
    {
    }

    /// <summary>Membuat exception dengan pesan dan penyebab.</summary>
    public NotConnectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
