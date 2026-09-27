using SyncNet.Helpers;
using SyncNet.Models.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SyncNet.Common
{
    class AppConfig
    {
        public static bool IsWindows { get; set; }

        public static string ConnectionString { get; set; }
        public static string SecretKey { get; set; }

        public static string SerialNumber { get; set; }
        public static string LicenseKey { get; set; }

        public static string HsmUrl { get; set; }
        public static int HsmTimeout { get; set; }
        public static int HsmReconnectDelay { get; set; }

        public static string AppDir { get; set; }
        public static string LogDir { get; set; }
        public static string TraceDir { get; set; }

        public static int MaxWorker { get; set; }
        public static int MaxRetryLog { get; set; }
        public static int MaxDaySettlement { get; set; }

        public static void Initialize()
        {
            //check platform
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // baca file JSON
            string filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(filePath);

            // deserialisasi JSON ke objek RootConfig
            var config = JsonSerializer.Deserialize<ConfigModel>(json);

            // Paths sesuai OS
            if (IsWindows)
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

            // Init creden
            CredenHelper.Initialize();

            // Database
            string Server = config.Database.Server;
            string Name = config.Database.Name;
            string User = config.Database.User;
            string Password = config.Database.Password.DecryptValue();
            int Port = config.Database.Port ?? 5432;

            // Build connection string
            ConnectionString = $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";

            // HSM
            HsmUrl = config.Hsm.Url ?? "http://127.0.0.1/hsm";
            HsmReconnectDelay = config.Hsm.ReconnectDelay ?? 5;
            HsmTimeout = config.Hsm.Timeout ?? 5;

            // Retry Policy
            MaxRetryLog = config.RetryPolicy.MaxRetryLog ?? 10;
            MaxDaySettlement = config.RetryPolicy.MaxDaySettlement ?? 14;

            // License
            SerialNumber = config.License.SerialNumber;
            LicenseKey = config.License.LicenseKey;

            // Worker default = jumlah core CPU
            MaxWorker = Environment.ProcessorCount;

            //DEBUG
            Console.WriteLine($"Platform: {(IsWindows ? "Windows" : "Linux")}");
            Console.WriteLine($"License Key: {LicenseKey}");
            Console.WriteLine($"HSM address: {HsmUrl}");
        }
    }
}
