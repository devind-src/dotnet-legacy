using Microsoft.Extensions.Configuration;
using SyncNet.Helpers;
using SyncNet.Logging;
using SyncNet.Models;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SyncNet.Common
{
    public class AppConfig
    {
        public const string APPNAME = "HSM Service";

        public static string ConnectionString { get; set; }
        public static string UrlListen { get; set; }

        public static string AppDir { get; set; }
        public static string LogDir { get; set; }
        public static string TraceDir { get; set; }

        public static bool TraceOn { get; set; }
        public static bool HsmEmulator { get; set; }

        public static string HsmConnectionMode { get; set; }
        public static string HsmIpAddress { get; set; }

        public static int HsmPort { get; set; }
        public static int HsmLenHeader { get; set; }
        public static int HsmTimeout { get; set; }
        public static int HsmReconnectDelay { get; set; }

        public static void Initialize(IConfiguration config)
        {
            // path OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDir = config.GetSection("Paths:Windows:App").Value ?? @"C:\SyncNet";
                LogDir = config.GetSection("Paths:Windows:Logs").Value ?? @"C:\SyncNet\Logs";
                TraceDir = config.GetSection("Paths:Windows:Traces").Value ?? @"C:\SyncNet\Traces";
            }
            else
            {
                AppDir = config.GetSection("Paths:Linux:App").Value ?? "/opt/SyncNet";
                LogDir = config.GetSection("Paths:Linux:Logs").Value ?? "/opt/SyncNet/Logs";
                TraceDir = config.GetSection("Paths:Linux:Traces").Value ?? "/opt/SyncNet/Traces";
            }

            Resources.Initialize();
            CredenHelper.Initialize();

            // konfigurasi HSM Service
            var hsmService = config.GetSection("HsmService");           
            UrlListen = hsmService["UrlListen"];
            HsmEmulator = hsmService["HsmEmulator"].ToBool();
            TraceOn = hsmService["TraceOn"].ToBool();

            // konfigurasi HSM Device
            var hsmDevice = config.GetSection("HsmDevice");
            HsmConnectionMode = hsmDevice["HsmConnectionMode"];
            HsmIpAddress = hsmDevice["HsmIpAddress"];
            HsmPort = hsmDevice["HsmPort"].ToNumber();
            HsmLenHeader = hsmDevice["HsmLenHeader"].ToNumber();
            HsmTimeout = hsmDevice["HsmTimeout"].ToNumber();
            HsmReconnectDelay = hsmDevice["HsmReconnectDelay"].ToNumber();

            // konfigurasi Core
            LoadConfigCore(AppDir);
        }

        public static void LoadConfigCore(string AppDir)
        {
            string fileConfig = string.Empty;

            //check platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileConfig = $@"{AppDir}\Core\Bin\appsettings.json";
            else
                fileConfig = $@"{AppDir}/Core/Bin/appsettings.json";

            // baca file JSON
            if (File.Exists(fileConfig) == false)
            {
                throw new FileNotFoundException($"File konfigurasi tidak ditemukan: {fileConfig}");
            }

            string json = File.ReadAllText(fileConfig);

            // deserialisasi JSON ke objek RootConfig
            var config = JsonSerializer.Deserialize<ConfigModel>(json);

            // Database
            string Server = config.Database.Server;
            string Name = config.Database.Name;
            string User = config.Database.User;
            string Password = config.Database.Password.DecryptValue();
            int Port = config.Database.Port ?? 5432;

            // Build connection string
            ConnectionString = $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";
         }      
    }
}
