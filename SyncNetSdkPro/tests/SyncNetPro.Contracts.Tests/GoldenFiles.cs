using System.Text;

namespace SyncNetPro.Contracts.Tests;

/// <summary>Akses golden file hasil <c>tools/SyncNetPro.GoldenGenerator</c> (dihasilkan SDK lama).</summary>
internal static class GoldenFiles
{
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "Golden");

    public static string ReadText(string fileName) =>
        File.ReadAllText(Path.Combine(Directory, fileName), new UTF8Encoding(false));

    public static byte[] ReadBytes(string fileName) => File.ReadAllBytes(Path.Combine(Directory, fileName));

    public static TheoryData<string> Cases(string prefix)
    {
        var data = new TheoryData<string>();
        foreach (string file in System.IO.Directory.GetFiles(Directory, prefix + "*.frame.bin").Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(file)[..^".frame.bin".Length]);
        }

        return data;
    }

    public static TheoryData<string> FromCases()
    {
        var data = new TheoryData<string>();
        foreach (string file in System.IO.Directory.GetFiles(Directory, "from-*.request.json").Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(file)[..^".request.json".Length]);
        }

        return data;
    }
}
