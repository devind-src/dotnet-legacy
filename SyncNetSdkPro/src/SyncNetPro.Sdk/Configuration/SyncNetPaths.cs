using System.Runtime.InteropServices;

namespace SyncNetPro.Sdk.Configuration;

/// <summary>
/// Resolusi folder instalasi SyncNet — satu-satunya tempat percabangan OS (dok. 05 §2).
/// Semua path lain dibentuk relatif terhadap <see cref="Home"/> dengan <see cref="Path.Combine(string[])"/>.
/// </summary>
public sealed class SyncNetPaths
{
    /// <summary>Nama variabel lingkungan untuk folder instalasi.</summary>
    public const string HomeEnvironmentVariable = "SYNCNET_HOME";

    /// <summary>Membuat resolusi path.</summary>
    /// <param name="configuredHome">Nilai <c>SyncNet:Home</c> (opsional).</param>
    public SyncNetPaths(string? configuredHome)
    {
        Home = ResolveHome(configuredHome, Environment.GetEnvironmentVariable(HomeEnvironmentVariable));
    }

    /// <summary>Folder instalasi.</summary>
    public string Home { get; }

    /// <summary><c>{Home}/Core/Bin/appsettings.json</c> — konfigurasi Core.</summary>
    public string CoreConfigFile => Path.Combine(Home, "Core", "Bin", "appsettings.json");

    /// <summary><c>{Home}/Core/Bin/Resources.bin</c> — kunci terenkripsi.</summary>
    public string ResourcesFile => Path.Combine(Home, "Core", "Bin", "Resources.bin");

    /// <summary><c>{Home}/Keys/PrivateKey.pem</c>.</summary>
    public string PrivateKeyFile => Path.Combine(Home, "Keys", "PrivateKey.pem");

    /// <summary><c>{Home}/Logs</c>.</summary>
    public string DefaultLogDirectory => Path.Combine(Home, "Logs");

    /// <summary><c>{Home}/Traces</c>.</summary>
    public string DefaultTraceDirectory => Path.Combine(Home, "Traces");

    /// <summary>Default OS: <c>C:\SyncNet</c> (Windows) atau <c>/opt/SyncNet</c>.</summary>
    public static string DefaultHome =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\SyncNet" : "/opt/SyncNet";

    /// <summary>Urutan: nilai konfigurasi → env <c>SYNCNET_HOME</c> → default OS.</summary>
    public static string ResolveHome(string? configured, string? environment)
    {
        string home = !string.IsNullOrWhiteSpace(configured) ? configured
            : !string.IsNullOrWhiteSpace(environment) ? environment
            : DefaultHome;

        return Path.GetFullPath(home);
    }
}
