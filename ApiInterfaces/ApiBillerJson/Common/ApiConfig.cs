using Microsoft.Extensions.Configuration;
using SyncNet.Helpers;
using System;
using System.IO;

namespace ApiBiller.Common
{
    internal class ApiConfig
    {
        // Urls
        public static string UrlBase { get; set; }

        // Settings
        public static int TimeoutSeconds { get; set; }

        // Identity
        public static string AcqInstId { get; set; }
        public static string FwdInstId { get; set; }
     
        //Flags
        public static bool DbMemory { get; set; }

        //Credentials
        public static string ClientId { get; set; }
        public static string SecretKey { get; set; }


        public static void Initialize()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Urls
            UrlBase = config["Urls:UrlBase"];

            // Settings
            TimeoutSeconds = config["Settings:TimeoutSeconds"].ToNumber();

            // Identity
            AcqInstId = config["Identity:AcqInstId"];
            FwdInstId = config["Identity:FwdInstId"];

            // Flags
            DbMemory = config["Flags:DbMemory"].ToBool();

            // Credentials
            ClientId = config["Credentials:ClientId"];
            SecretKey = config["Credentials:SecretKey"];
        }
    }
}
