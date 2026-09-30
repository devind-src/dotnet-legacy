using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Npgsql;
using SyncNetPro.Sdk.Configuration;
using SyncNetPro.Sdk.Logging;
using SyncNetPro.Sdk.Tests.Infrastructure;

namespace SyncNetPro.Sdk.Tests;

public class SyncNetPathsTests
{
    [Fact]
    public void Configured_home_wins_over_environment()
    {
        string configured = Path.Combine(Path.GetTempPath(), "cfg");
        Assert.Equal(Path.GetFullPath(configured), SyncNetPaths.ResolveHome(configured, "/env/home"));
    }

    [Fact]
    public void Environment_is_used_when_not_configured()
    {
        string env = Path.Combine(Path.GetTempPath(), "env");
        Assert.Equal(Path.GetFullPath(env), SyncNetPaths.ResolveHome(null, env));
    }

    [Fact]
    public void Falls_back_to_os_default()
    {
        string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\SyncNet" : "/opt/SyncNet";
        Assert.Equal(Path.GetFullPath(expected), SyncNetPaths.ResolveHome(" ", null));
    }

    [Fact]
    public void Core_files_are_relative_to_home()
    {
        using var home = new TempDirectory();
        var paths = new SyncNetPaths(home.Path);

        Assert.Equal(home.Combine("Core", "Bin", "appsettings.json"), paths.CoreConfigFile);
        Assert.Equal(home.Combine("Core", "Bin", "Resources.bin"), paths.ResourcesFile);
        Assert.Equal(home.Combine("Keys", "PrivateKey.pem"), paths.PrivateKeyFile);
    }
}

public class LegacyCoreConfigurationTests
{
    private static readonly byte[] ConfigKey = RandomNumberGenerator.GetBytes(32);
    private static readonly byte[] ConfigIv = RandomNumberGenerator.GetBytes(16);

    /// <summary>Membuat instalasi Core tiruan dengan format file SDK lama.</summary>
    private static void WriteCoreInstallation(string home, string? encryptedPassword = null, bool rabbit = false)
    {
        Directory.CreateDirectory(Path.Combine(home, "Core", "Bin"));
        Directory.CreateDirectory(Path.Combine(home, "Keys"));

        using RSA rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(home, "Keys", "PrivateKey.pem"), rsa.ExportRSAPrivateKeyPem());

        string resources = JsonConvert.SerializeObject(new
        {
            AES = new { Config = new { Key = Convert.ToHexString(ConfigKey), IV = Convert.ToHexString(ConfigIv) } },
            DES = new { Database = "x", Config = "y", HSM = new { LMK_1 = "a", LMK_2 = "b" } },
        });

        // Format hybrid Resources.bin: [int32 len][RSA(aesKey)][nonce 12][tag 16][cipher]
        byte[] aesKey = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plain = Encoding.UTF8.GetBytes(resources);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];
        using (var gcm = new AesGcm(aesKey, 16)) gcm.Encrypt(nonce, plain, cipher, tag);
        byte[] encryptedKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

        using (var fs = File.Create(Path.Combine(home, "Core", "Bin", "Resources.bin")))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(encryptedKey.Length);
            writer.Write(encryptedKey);
            writer.Write(nonce);
            writer.Write(tag);
            writer.Write(cipher);
        }

        string config = JsonConvert.SerializeObject(new
        {
            Database = new { Server = "db.local", Name = "syncnet", User = "app", Password = encryptedPassword ?? Encrypt("s3cr3t!"), Port = 5433 },
            Paths = new
            {
                Windows = new { App = @"C:\SyncNet", Logs = @"D:\Logs", Traces = @"D:\Traces" },
                Linux = new { App = "/opt/SyncNet", Logs = "/var/log/syncnet", Traces = "/var/log/syncnet-traces" },
            },
            Hsm = new { Url = "http://127.0.0.1:40001" },
            RabbitMQ = rabbit ? new { Enable = true, HostName = "mq", Port = 5673, QueueName = "q1" } : null,
        });
        File.WriteAllText(Path.Combine(home, "Core", "Bin", "appsettings.json"), config);
    }

    /// <summary>Setara <c>CredenHelper.EncryptValue</c> SDK lama.</summary>
    private static string Encrypt(string plain)
    {
        using Aes aes = Aes.Create();
        aes.Key = ConfigKey;
        return Convert.ToBase64String(aes.EncryptCbc(Encoding.UTF8.GetBytes(plain), ConfigIv));
    }

    [Fact]
    public void Reads_legacy_core_configuration_and_decrypts_password()
    {
        using var home = new TempDirectory();
        WriteCoreInstallation(home.Path, rabbit: true);

        CoreEnvironment env = LegacyCoreConfiguration.Load(new SyncNetOptions { AppName = "x", Home = home.Path });

        var cs = new NpgsqlConnectionStringBuilder(env.ConnectionString);
        Assert.Equal("db.local", cs.Host);
        Assert.Equal(5433, cs.Port);
        Assert.Equal("syncnet", cs.Database);
        Assert.Equal("app", cs.Username);
        Assert.Equal("s3cr3t!", cs.Password);
        Assert.True(env.CoreConfigurationLoaded);
        Assert.Equal(new RabbitMqSettings("mq", 5673, "q1"), env.RabbitMq);
        Assert.Equal("http://127.0.0.1:40001", env.HsmUrl);

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        Assert.Equal(windows ? @"D:\Logs" : "/var/log/syncnet", env.LogDirectory);
        Assert.Equal(windows ? @"D:\Traces" : "/var/log/syncnet-traces", env.TraceDirectory);
    }

    [Fact]
    public void Options_override_directories_and_connection_string()
    {
        using var home = new TempDirectory();
        WriteCoreInstallation(home.Path);
        var options = new SyncNetOptions { AppName = "x", Home = home.Path };
        options.Logging.Directory = home.Combine("mylogs");
        options.Database.ConnectionString = "Host=override;Database=d";

        CoreEnvironment env = LegacyCoreConfiguration.Load(options);

        Assert.Equal("Host=override;Database=d", env.ConnectionString);
        Assert.Equal(home.Combine("mylogs"), env.LogDirectory);
    }

    [Fact]
    public void Invalid_encrypted_password_fails_loudly()
    {
        using var home = new TempDirectory();
        WriteCoreInstallation(home.Path, encryptedPassword: Convert.ToBase64String(new byte[16]));

        var ex = Assert.Throws<InvalidDataException>(() => LegacyCoreConfiguration.Load(new SyncNetOptions { AppName = "x", Home = home.Path }));
        Assert.Contains("didekripsi", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_mode_without_core_configuration_explains_what_to_do()
    {
        using var home = new TempDirectory();

        var ex = Assert.Throws<FileNotFoundException>(() => LegacyCoreConfiguration.Load(new SyncNetOptions { AppName = "x", Home = home.Path }));
        Assert.Contains("NodeSource=Json", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_mode_runs_without_core_installation()
    {
        using var home = new TempDirectory();

        CoreEnvironment env = LegacyCoreConfiguration.Load(new SyncNetOptions { AppName = "x", Home = home.Path, NodeSource = NodeSourceKind.Json });

        Assert.Null(env.ConnectionString);
        Assert.False(env.CoreConfigurationLoaded);
        Assert.Equal(home.Combine("Logs"), env.LogDirectory);
        Assert.Equal(home.Combine("Traces"), env.TraceDirectory);
    }
}

public class LogFileNamesTests
{
    [Theory]
    [InlineData("API Biller", "api-biller")]
    [InlineData("BILLER_ABC", "biller_abc")]
    [InlineData("  Log  Services ", "log--services")]
    [InlineData("a/b\\c:d*e?f\"g<h>i|j", "abcdefghij")]
    [InlineData("", "syncnet")]
    public void Normalizes_the_same_way_on_every_os(string input, string expected) =>
        Assert.Equal(expected, LogFileNames.Normalize(input));
}
