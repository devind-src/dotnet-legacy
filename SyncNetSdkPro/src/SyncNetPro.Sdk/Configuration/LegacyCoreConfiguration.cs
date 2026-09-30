using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace SyncNetPro.Sdk.Configuration;

/// <summary>
/// Membaca konfigurasi Core format lama (<c>Core/Bin/appsettings.json</c>, <c>Resources.bin</c>,
/// <c>Keys/PrivateKey.pem</c>) tanpa perlu mengubah file apa pun di server (dok. 05 §2).
/// </summary>
public static class LegacyCoreConfiguration
{
    /// <summary>Membentuk <see cref="CoreEnvironment"/> dari opsi interface dan (bila ada) config Core.</summary>
    /// <exception cref="FileNotFoundException">Mode database tanpa connection string dan config Core tidak ada.</exception>
    /// <exception cref="InvalidDataException">Config Core/Resources tidak valid atau password tidak bisa didekripsi.</exception>
    public static CoreEnvironment Load(SyncNetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var paths = new SyncNetPaths(options.Home);
        bool needsDatabase = options.NodeSource == NodeSourceKind.Database && string.IsNullOrWhiteSpace(options.Database.ConnectionString);

        if (!File.Exists(paths.CoreConfigFile))
        {
            if (needsDatabase)
            {
                throw new FileNotFoundException(
                    $"Konfigurasi Core tidak ditemukan: {paths.CoreConfigFile}. Isi SyncNet:Home / env {SyncNetPaths.HomeEnvironmentVariable}, " +
                    "atau SyncNet:Database:ConnectionString, atau gunakan SyncNet:NodeSource=Json untuk pengembangan.",
                    paths.CoreConfigFile);
            }

            return new CoreEnvironment
            {
                Paths = paths,
                ConnectionString = NullIfEmpty(options.Database.ConnectionString),
                LogDirectory = options.Logging.Directory ?? paths.DefaultLogDirectory,
                TraceDirectory = options.Logging.TraceDirectory ?? paths.DefaultTraceDirectory,
            };
        }

        JObject config = ReadJson(paths.CoreConfigFile);
        (string? logs, string? traces) = ReadOsPaths(config);

        string? connectionString = NullIfEmpty(options.Database.ConnectionString);
        if (connectionString is null && config["Database"] is JObject db)
        {
            ResourcesKeys keys = ReadResources(paths);
            connectionString = BuildConnectionString(db, keys);
        }

        RabbitMqSettings? rabbit = null;
        if (config["RabbitMQ"] is JObject r && r.Value<bool?>("Enable") == true)
        {
            rabbit = new RabbitMqSettings(
                r.Value<string>("HostName") ?? "localhost",
                r.Value<int?>("Port") is int port and > 0 ? port : 5672,
                r.Value<string>("QueueName") ?? "transaction_logs_queue");
        }

        return new CoreEnvironment
        {
            Paths = paths,
            ConnectionString = connectionString,
            LogDirectory = options.Logging.Directory ?? logs ?? paths.DefaultLogDirectory,
            TraceDirectory = options.Logging.TraceDirectory ?? traces ?? paths.DefaultTraceDirectory,
            RabbitMq = rabbit,
            HsmUrl = config["Hsm"]?.Value<string>("Url"),
            CoreConfigurationLoaded = true,
        };
    }

    internal sealed record ResourcesKeys(byte[] ConfigKey, byte[] ConfigIv);

    /// <summary>Dekripsi hybrid <c>Resources.bin</c>: RSA-OAEP-SHA256 (kunci AES) + AES-256-GCM (isi).</summary>
    internal static ResourcesKeys ReadResources(SyncNetPaths paths)
    {
        if (!File.Exists(paths.ResourcesFile)) throw new FileNotFoundException("File Resources.bin tidak ditemukan.", paths.ResourcesFile);
        if (!File.Exists(paths.PrivateKeyFile)) throw new FileNotFoundException("File private key tidak ditemukan.", paths.PrivateKeyFile);

        string json;
        try
        {
            using var fs = File.OpenRead(paths.ResourcesFile);
            using var reader = new BinaryReader(fs);

            int keyLength = reader.ReadInt32();
            byte[] encryptedKey = reader.ReadBytes(keyLength);
            byte[] nonce = reader.ReadBytes(12);
            byte[] tag = reader.ReadBytes(16);
            byte[] cipher = reader.ReadBytes((int)(fs.Length - fs.Position));

            using RSA rsa = RSA.Create();
            ImportPrivateKey(rsa, paths.PrivateKeyFile);
            byte[] aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

            byte[] plain = new byte[cipher.Length];
            using (var gcm = new AesGcm(aesKey, 16))
            {
                gcm.Decrypt(nonce, cipher, tag, plain);
            }

            json = Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is CryptographicException or EndOfStreamException or IOException or ArgumentException)
        {
            throw new InvalidDataException($"Resources.bin tidak dapat didekripsi: {ex.Message}", ex);
        }

        JObject resources = JObject.Parse(json);
        string? key = (string?)resources.SelectToken("AES.Config.Key");
        string? iv = (string?)resources.SelectToken("AES.Config.IV");
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            throw new InvalidDataException("Resources.bin tidak memuat AES.Config.Key/IV.");

        return new ResourcesKeys(Convert.FromHexString(key), Convert.FromHexString(iv));
    }

    /// <summary>Dekripsi nilai konfigurasi (AES-CBC/PKCS7, Base64) — setara <c>CredenHelper.DecryptValue</c>.</summary>
    /// <exception cref="InvalidDataException">Nilai tidak dapat didekripsi (SDK lama diam-diam mengembalikan string kosong).</exception>
    internal static string DecryptValue(string cipherText, ResourcesKeys keys)
    {
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = keys.ConfigKey;
            aes.IV = keys.ConfigIv;
            byte[] plain = aes.DecryptCbc(Convert.FromBase64String(cipherText), keys.ConfigIv, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new InvalidDataException("Nilai terenkripsi di konfigurasi Core tidak dapat didekripsi.", ex);
        }
    }

    private static string BuildConnectionString(JObject db, ResourcesKeys keys)
    {
        string? password = db.Value<string>("Password");
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = db.Value<string>("Server"),
            Port = db.Value<int?>("Port") ?? 5432,
            Database = db.Value<string>("Name"),
            Username = db.Value<string>("User"),
            Password = string.IsNullOrEmpty(password) ? password : DecryptValue(password, keys),
        };
        return builder.ConnectionString;
    }

    private static (string? Logs, string? Traces) ReadOsPaths(JObject config)
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Linux";
        JToken? paths = config["Paths"]?[os];
        return (NullIfEmpty(paths?.Value<string>("Logs")), NullIfEmpty(paths?.Value<string>("Traces")));
    }

    private static JObject ReadJson(string file)
    {
        try
        {
            return JObject.Parse(File.ReadAllText(file));
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Konfigurasi Core tidak valid: {file}: {ex.Message}", ex);
        }
    }

    private static void ImportPrivateKey(RSA rsa, string file)
    {
        string text = File.ReadAllText(file);
        if (text.Contains("-----BEGIN", StringComparison.Ordinal)) rsa.ImportFromPem(text);
        else rsa.ImportRSAPrivateKey(File.ReadAllBytes(file), out _);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
