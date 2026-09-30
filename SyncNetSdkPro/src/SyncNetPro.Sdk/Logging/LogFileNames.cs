using System.Runtime.InteropServices;
using System.Text;

namespace SyncNetPro.Sdk.Logging;

/// <summary>
/// Normalisasi nama file log/trace yang sama di semua OS (keputusan Q8): huruf kecil, spasi → <c>-</c>,
/// karakter yang tidak valid di Windows/Linux dihapus.
/// </summary>
public static class LogFileNames
{
    private static readonly char[] Invalid = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>Menormalisasi nama.</summary>
    /// <param name="name">Nama (mis. nama aplikasi atau node).</param>
    /// <param name="legacyWindowsNames">Di Windows, pertahankan nama asli (perilaku lama).</param>
    public static string Normalize(string name, bool legacyWindowsNames = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return "syncnet";
        if (legacyWindowsNames && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return Strip(name);

        var sb = new StringBuilder(name.Length);
        foreach (char c in name.Trim())
        {
            if (char.IsWhiteSpace(c)) sb.Append('-');
            else if (c >= 32 && Array.IndexOf(Invalid, c) < 0) sb.Append(char.ToLowerInvariant(c));
        }

        return sb.Length == 0 ? "syncnet" : sb.ToString();
    }

    private static string Strip(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (char c in name) if (c >= 32 && Array.IndexOf(Invalid, c) < 0) sb.Append(c);
        return sb.Length == 0 ? "syncnet" : sb.ToString();
    }
}
