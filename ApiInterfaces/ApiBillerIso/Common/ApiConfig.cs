using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ApiBiller.Common
{
    internal class ApiConfig
    {

        // Identity
        public static string AcqInstId { get; set; }
        public static string FwdInstId { get; set; }

        //Flags
        public static bool NonPersistent { get; set; }
        public static bool DbMemory { get; set; }
        public static bool LogEchoTest { get; set; }

        public static void Initialize()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Identity
            AcqInstId = config["Identity:AcqInstId"];
            FwdInstId = config["Identity:FwdInstId"];

            // Flags
            NonPersistent = bool.Parse(config["Flags:NonPersistent"]);
            DbMemory = bool.Parse(config["Flags:DbMemory"]);
            LogEchoTest = bool.Parse(config["Flags:LogEchoTest"]);
        }
    }
}
