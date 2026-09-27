namespace SyncNet.Models.Common
{
    internal class ConfigModel
    {
        public DatabaseConfig Database { get; set; }
        public PathsConfig Paths { get; set; }
        public HsmConfig Hsm { get; set; }
        public RetryPolicyConfig RetryPolicy { get; set; }
        public LicenseConfig License { get; set; }
    }

    public class DatabaseConfig
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int? Port { get; set; }
    }

    public class PathsConfig
    {
        public OsPaths Windows { get; set; }
        public OsPaths Linux { get; set; }
    }

    public class OsPaths
    {
        public string App { get; set; }
        public string Logs { get; set; }
        public string Traces { get; set; }
    }

    public class HsmConfig
    {
        public string Url { get; set; }
        public int? Timeout { get; set; }
        public int? ReconnectDelay { get; set; }
    }

    public class RetryPolicyConfig
    {
        public int? MaxRetryLog { get; set; }
        public int? MaxDaySettlement { get; set; }
    }

    public class LicenseConfig
    {
        public string SerialNumber { get; set; }
        public string LicenseKey { get; set; }
    }
}
