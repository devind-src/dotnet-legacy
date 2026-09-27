using Microsoft.Extensions.Configuration;
using System;

namespace ApiChannel.Common
{
    internal class ApiConfig
    {
        // Credentials
        public static string ClientID { get; set; }
        public static string SecretKey { get; set; }

        // Identity
        public static string AcqInstId { get; set; }
        public static string FwdInstId { get; set; }


        //Flags
        public static bool DbMemory { get; set; }
        public static bool UpdateLastEcho { get; set; }

        public static void Initialize()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Credentials
            ClientID = config["Credentials:ClientID"];
            SecretKey = config["Credentials:SecretKey"];

            // Identity
            AcqInstId = config["Identity:AcqInstId"];
            FwdInstId = config["Identity:FwdInstId"];

            // Flags
            DbMemory = bool.Parse(config["Flags:DbMemory"]);
            UpdateLastEcho = bool.Parse(config["Flags:UpdateLastEcho"]);
        }
    }
}
