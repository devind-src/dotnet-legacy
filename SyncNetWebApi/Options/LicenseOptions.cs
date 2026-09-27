namespace SyncNetApi.Options
{
    public class LicenseOptions
    {
        public const string SectionName = "License";

        public string SerialNumber { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
    }
}
