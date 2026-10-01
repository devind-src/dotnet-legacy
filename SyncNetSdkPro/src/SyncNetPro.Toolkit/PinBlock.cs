using System.Globalization;

namespace SyncNetPro.Toolkit;

/// <summary>Format PIN block (<c>NbCard.EnumPinblockFormat</c> SDK lama).</summary>
public enum PinBlockFormat
{
    /// <summary>ISO 9564 format 0 / ANSI X9.8 (<c>FORMAT_01_ANSI</c>).</summary>
    Iso0 = 1,

    /// <summary>Docutel (<c>FORMAT_02_DOCUTEL</c>), PIN 4–6 digit.</summary>
    Docutel = 2,

    /// <summary>IBM 3624 (<c>FORMAT_03_IBM</c>).</summary>
    Ibm3624 = 3,

    /// <summary>PLUS (<c>FORMAT_04_PLUS</c>).</summary>
    Plus = 4,
}

/// <summary>Membuat PIN block terenkripsi (<c>NbCard.CreatePinBlock</c> SDK lama).</summary>
public static class PinBlock
{
    /// <summary>PIN block terenkripsi (hex) dengan kunci DES/3DES.</summary>
    /// <exception cref="ArgumentException">PIN/PAN tidak valid (SDK lama tetap mengenkripsi walau validasi gagal).</exception>
    public static string Create(string pan, string pin, string hexKey, PinBlockFormat format) =>
        DesEcb.EncryptHex(CreateClear(pan, pin, format), hexKey);

    /// <summary>PIN block sebelum enkripsi (hex 16 digit).</summary>
    public static string CreateClear(string pan, string pin, PinBlockFormat format)
    {
        ArgumentNullException.ThrowIfNull(pin);
        if (pin.Length is < 4 or > 12 || !pin.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("PIN harus 4–12 digit angka.", nameof(pin));
        }

        switch (format)
        {
            case PinBlockFormat.Iso0:
                // Digit PAN paling kanan tanpa check digit (SDK lama memakai pan.Substring(3, 12): hanya benar untuk PAN 16 digit).
                RequirePan(pan);
                return Xor(PinField(pin), string.Concat("0000", pan.AsSpan(pan.Length - 13, 12)));

            case PinBlockFormat.Plus:
                RequirePan(pan);
                return Xor(PinField(pin), "0000" + pan[..12]);

            case PinBlockFormat.Docutel:
                // SDK lama memotong PIN > 6 digit diam-diam sehingga PIN yang diverifikasi berbeda.
                if (pin.Length > 6) throw new ArgumentException("PIN Docutel maksimal 6 digit.", nameof(pin));
                return pin.Length.ToString(CultureInfo.InvariantCulture) + pin.PadRight(6, '0') + "987654321";

            case PinBlockFormat.Ibm3624:
                return pin.PadRight(16, 'F');

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Format PIN block tidak dikenal.");
        }
    }

    private static string PinField(string pin) => ("0" + pin.Length.ToString("X", CultureInfo.InvariantCulture) + pin).PadRight(16, 'F');

    private static void RequirePan(string pan)
    {
        ArgumentNullException.ThrowIfNull(pan);
        if (pan.Length is < 13 or > 19 || !pan.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("PAN harus 13–19 digit angka.", nameof(pan));
        }
    }

    private static string Xor(string a, string b)
    {
        byte[] x = Convert.FromHexString(a);
        byte[] y = Convert.FromHexString(b);
        for (int i = 0; i < x.Length; i++) x[i] ^= y[i];
        return Convert.ToHexString(x);
    }
}

/// <summary>Luhn (mod 10) untuk nomor kartu.</summary>
public static class Luhn
{
    /// <summary>Check digit untuk <paramref name="digits"/> (tanpa check digit).</summary>
    public static int CheckDigit(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) throw new ArgumentException("Hanya digit.", nameof(digits));

        int sum = 0;
        bool doubleIt = true;
        for (int i = digits.Length - 1; i >= 0; i--, doubleIt = !doubleIt)
        {
            int d = digits[i] - '0';
            if (doubleIt) d = d * 2 > 9 ? (d * 2) - 9 : d * 2;
            sum += d;
        }

        return (10 - (sum % 10)) % 10;
    }

    /// <summary>Menambahkan check digit.</summary>
    public static string Append(string digits) => digits + CheckDigit(digits).ToString(CultureInfo.InvariantCulture);

    /// <summary>PAN 12–19 digit dengan check digit benar (SDK lama hanya menerima 13–16 digit).</summary>
    public static bool IsValid(string? pan) =>
        pan is { Length: >= 12 and <= 19 } && pan.All(char.IsAsciiDigit) && CheckDigit(pan[..^1]) == pan[^1] - '0';
}
