using SyncNet.Helpers;
using SyncNet.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SyncNet.Common
{
    public class AppConfig
    {
        public static string ConnectionString { get; set; }

        public static string AppDir { get; set; }
        public static string LogDir { get; set; }
        public static string TraceDir { get; set; }

        public static SyncNetConfig Settings { get; set; } = new SyncNetConfig();

        public static void Initialize()
        {
            // baca file JSON untuk mendapatkan AppDir
            string filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string jsonBaseConfig = File.ReadAllText(filePath);

            // deserialisasi JSON ke objek BaseConfig
            var baseConfig = JsonSerializer.Deserialize<BaseConfig>(jsonBaseConfig);

            // path OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDir = baseConfig?.AppDir?.Windows ?? @"C:\SyncNet";
            }
            else
            {
                AppDir = baseConfig?.AppDir?.Linux ?? "/opt/SyncNet";
            }

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
            Settings = JsonSerializer.Deserialize<SyncNetConfig>(json);

            // Database
            string Server = Settings.Database.Server;
            string Name = Settings.Database.Name;
            string User = Settings.Database.User;
            string Password = Settings.Database.Password.DecryptValue();
            int Port = Settings.Database.Port;

            //log & trace dir
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LogDir = Settings.Paths.Windows.Logs;
                TraceDir = Settings.Paths.Windows.Traces;
            }
            else
            {
                LogDir = Settings.Paths.Linux.Logs;
                TraceDir = Settings.Paths.Linux.Traces;
            }

            // Build connection string
            ConnectionString = $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";
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
