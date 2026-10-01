using System.Security.Cryptography;

namespace SyncNetPro.Toolkit;

/// <summary>
/// DES / 3DES mode ECB tanpa padding (blok PIN, KCV, key exchange). Panjang kunci menentukan algoritma:
/// 8 byte = DES, 16/24 byte = 3DES (<c>DesAlgorithm.EncryptHex</c> SDK lama).
/// </summary>
public static class DesEcb
{
    /// <summary>Enkripsi data hex dengan kunci hex.</summary>
    /// <exception cref="ArgumentException">Kunci bukan 16/32/48 digit hex atau data bukan kelipatan 16 digit hex.</exception>
    public static string EncryptHex(string hexData, string hexKey) =>
        Convert.ToHexString(Encrypt(FromHex(hexData, nameof(hexData)), FromHex(hexKey, nameof(hexKey))));

    /// <summary>Dekripsi data hex dengan kunci hex.</summary>
    public static string DecryptHex(string hexData, string hexKey) =>
        Convert.ToHexString(Decrypt(FromHex(hexData, nameof(hexData)), FromHex(hexKey, nameof(hexKey))));

    /// <summary>Enkripsi ECB.</summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        using SymmetricAlgorithm cipher = Create(key, data.Length);
        return cipher.EncryptEcb(data, PaddingMode.None);
    }

    /// <summary>Dekripsi ECB.</summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        using SymmetricAlgorithm cipher = Create(key, data.Length);
        return cipher.DecryptEcb(data, PaddingMode.None);
    }

    private static SymmetricAlgorithm Create(ReadOnlySpan<byte> key, int dataLength)
    {
        // SDK lama mengembalikan "0000000000000000" diam-diam untuk kunci yang salah panjang.
        if (key.Length is not (8 or 16 or 24))
        {
            throw new ArgumentException($"Panjang kunci DES/3DES harus 8, 16, atau 24 byte, bukan {key.Length}.", nameof(key));
        }

        if (dataLength == 0 || dataLength % 8 != 0)
        {
            throw new ArgumentException($"Data harus kelipatan 8 byte, bukan {dataLength}.", nameof(dataLength));
        }

#pragma warning disable CA5350, CA5351 // DES/3DES diwajibkan oleh standar PIN block & sistem remote
        SymmetricAlgorithm cipher = key.Length == 8 ? DES.Create() : TripleDES.Create();
#pragma warning restore CA5350, CA5351
        // Kunci 3DES double-length (K1K2) diperluas eksplisit menjadi K1K2K1: OpenSSL (Linux) menolak kunci 16 byte
        // pada operasi one-shot, sedangkan Windows CNG menerimanya — hasil harus sama di semua OS.
        cipher.Key = key.Length == 16 ? [.. key, .. key[..8]] : key.ToArray();
        return cipher;
    }

    private static byte[] FromHex(string hex, string name)
    {
        ArgumentNullException.ThrowIfNull(hex, name);
        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"{name} bukan hex yang valid.", name, ex);
        }
    }
}

/// <summary>Key Check Value: enkripsi blok nol dengan kunci.</summary>
public static class KeyCheckValue
{
    /// <summary>KCV hex; <paramref name="length"/> digit pertama (default 16 seperti SDK lama, umumnya dipakai 6).</summary>
    public static string Compute(string hexKey, int length = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 16);
        return DesEcb.EncryptHex("0000000000000000", hexKey)[..length];
    }
}
