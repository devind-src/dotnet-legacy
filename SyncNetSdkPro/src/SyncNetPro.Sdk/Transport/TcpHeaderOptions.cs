namespace SyncNetPro.Sdk.Transport;

/// <summary>Jenis header panjang pesan TCP.</summary>
public enum TcpHeaderType
{
    /// <summary>2 byte biner (maks. 65.535).</summary>
    Binary2Byte,

    /// <summary>2 byte BCD, 4 digit desimal (maks. 9.999).</summary>
    Bcd2Byte,

    /// <summary>4 byte biner.</summary>
    Binary4Byte,

    /// <summary>4 karakter ASCII digit (maks. 9.999).</summary>
    Ascii4Digit,
}

/// <summary>Isi nilai header panjang.</summary>
public enum TcpLengthMode
{
    /// <summary>Panjang payload saja.</summary>
    Exclude,

    /// <summary>Panjang payload + header.</summary>
    Include,
}

/// <summary>Urutan byte header biner.</summary>
public enum TcpEndianMode
{
    /// <summary>Hi-Lo (network order).</summary>
    BigEndian,

    /// <summary>Lo-Hi.</summary>
    LittleEndian,
}

/// <summary>Opsi header panjang (setara <c>TcpHeader</c> SDK lama).</summary>
/// <param name="HeaderType">Jenis header.</param>
/// <param name="LengthMode">Include/exclude header.</param>
/// <param name="Endian">Urutan byte (hanya untuk header biner).</param>
/// <param name="MaxPayloadLength">Panjang payload maksimum yang diterima (default 65.535, sama dengan SDK lama).</param>
public sealed record TcpHeaderOptions(
    TcpHeaderType HeaderType = TcpHeaderType.Binary2Byte,
    TcpLengthMode LengthMode = TcpLengthMode.Exclude,
    TcpEndianMode Endian = TcpEndianMode.BigEndian,
    int MaxPayloadLength = ushort.MaxValue)
{
    /// <summary>
    /// Framing kanal Core, command port, dan Log Services: 2 byte biner big-endian, exclude.
    /// Selalu instance baru — deserializer (mis. Newtonsoft) dapat mengisi properti <c>init</c> lewat reflection,
    /// sehingga instance bersama bisa berubah tanpa disadari.
    /// </summary>
    public static TcpHeaderOptions Default => new();
}
