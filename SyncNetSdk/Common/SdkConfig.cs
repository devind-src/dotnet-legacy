using Newtonsoft.Json;
using SyncNet.Helpers;
using SyncNet.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SyncNet.Common
{
    public class SdkConfig
    {
        public static string AppDir { get; set; }
        public static string LogDir { get; set; }
        public static string TraceDir { get; set; }
        public static string ConnectionString { get; set; }
        public static ConfigModel Settings { get; set; } = new ConfigModel();


        public static void Initialize()
        {
            // baca file JSON untuk mendapatkan AppDir
            string filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string jsonBaseConfig = File.ReadAllText(filePath);

            // deserialisasi JSON ke objek BaseConfig
            var baseConfig = JsonConvert.DeserializeObject<BaseConfig>(jsonBaseConfig);
            if (baseConfig?.AppDir == null)
            {
                throw new InvalidDataException(
                    $"Konfigurasi dasar tidak valid: properti AppDir wajib tersedia pada {filePath}.");
            }

            //check platform
            string fileConfig;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDir = baseConfig.AppDir.Windows ?? "C:\\SyncNet";
                fileConfig = $@"{AppDir}\Core\Bin\appsettings.json";
            }
            else
            {
                AppDir = baseConfig.AppDir.Linux ?? "/opt/SyncNet";
                fileConfig = $@"{AppDir}/Core/Bin/appsettings.json";
            }

            // baca file JSON
            if (File.Exists(fileConfig) == false)
            {
                throw new FileNotFoundException($"File konfigurasi tidak ditemukan: {fileConfig}");
            }

            // baca file JSON untuk mendapatkan konfigurasi lengkap
            string json = File.ReadAllText(fileConfig);
            if (string.IsNullOrEmpty(json) == true)
            {
                throw new FileNotFoundException($"File konfigurasi tidak valid: {fileConfig}");
            }

            // deserialisasi JSON ke objek SyncNetConfig
            var config = JsonConvert.DeserializeObject<ConfigModel>(json);

            // validate config
            if (config?.Database == null || config.Paths == null ||
                config.Paths.Windows == null || config.Paths.Linux == null ||
                config.Hsm == null || config.RetryPolicy == null || config.License == null)
            {
                throw new InvalidDataException(
                    $"Konfigurasi tidak lengkap atau tidak valid: {fileConfig}.");
            }

            Resources.Initialize();
            CredenHelper.Initialize();

            // Database
            string Server = config.Database.Server;
            string Name = config.Database.Name;
            string User = config.Database.User;
            string Password = config.Database.Password.DecryptValue();
            int Port = config.Database.Port ?? 5432;

            // Build connection string
            ConnectionString = $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";

            // Paths sesuai OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDir = config.Paths.Windows.App ?? @"C:\SyncNet";
                LogDir = config.Paths.Windows.Logs ?? @"C:\SyncNet\Logs";
                TraceDir = config.Paths.Windows.Traces ?? @"C:\SyncNet\Traces";
            }
            else
            {
                AppDir = config.Paths.Linux.App ?? "/opt/SyncNet";
                LogDir = config.Paths.Linux.Logs ?? "/opt/SyncNet/Logs";
                TraceDir = config.Paths.Linux.Traces ?? "/opt/SyncNet/Traces";
            }

            // HSM
            Settings.Hsm.Url = config.Hsm.Url ?? "http://127.0.0.1/hsm";
            Settings.Hsm.ReconnectDelay = config.Hsm.ReconnectDelay ?? 5;
            Settings.Hsm.Timeout = config.Hsm.Timeout ?? 5;

            // Retry Policy
            Settings.RetryPolicy.MaxRetryLog = config.RetryPolicy.MaxRetryLog ?? 10;
            Settings.RetryPolicy.MaxDaySettlement = config.RetryPolicy.MaxDaySettlement ?? 14;

            // License
            Settings.License.SerialNumber = config.License.SerialNumber;
            Settings.License.LicenseKey = config.License.LicenseKey;

            // Paths Windows & Linux
            Settings.Paths.Windows.App = config.Paths.Windows.App;
            Settings.Paths.Windows.Logs = config.Paths.Windows.Logs;
            Settings.Paths.Windows.Traces = config.Paths.Windows.Traces;

            Settings.Paths.Linux.App = config.Paths.Linux.App;
            Settings.Paths.Linux.Logs = config.Paths.Linux.Logs;
            Settings.Paths.Linux.Traces = config.Paths.Linux.Traces;

            // RabbitMQ
            Settings.RabbitMQ.Enable = config.RabbitMQ.Enable;
            Settings.RabbitMQ.HostName = config.RabbitMQ.HostName;
            Settings.RabbitMQ.Port = config.RabbitMQ.Port;
            Settings.RabbitMQ.QueueName = config.RabbitMQ.QueueName;
        }

        public class BaseConfig
        {
            public BaseDirectory AppDir { get; set; } = new BaseDirectory();
            public class BaseDirectory
            {
                public string Windows { get; set; }
                public string Linux { get; set; }
            }
        }
    }
}
