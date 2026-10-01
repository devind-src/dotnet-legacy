namespace SyncNetPro.Iso8583;

/// <summary>Pesan ISO 8583 tidak sesuai spesifikasi (saat set field, pack, atau parse).</summary>
public sealed class IsoFormatException : FormatException
{
    /// <summary>Membuat exception.</summary>
    public IsoFormatException()
    {
    }

    /// <summary>Membuat exception.</summary>
    public IsoFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Membuat exception.</summary>
    public IsoFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Membuat exception untuk field tertentu.</summary>
    public IsoFormatException(string message, int? field, int? offset = null)
        : base(message)
    {
        Field = field;
        Offset = offset;
    }

    /// <summary>Nomor field penyebab (bila ada).</summary>
    public int? Field { get; }

    /// <summary>Posisi byte saat parse (bila ada).</summary>
    public int? Offset { get; }
}
