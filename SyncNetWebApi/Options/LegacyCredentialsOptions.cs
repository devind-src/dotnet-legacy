namespace SyncNetApi.Options
{
    public class LegacyCredentialsOptions
    {
        public const string SectionName = "LegacyCredentials";

        /// <summary>Path to the legacy app's encrypted Resources.bin (hybrid RSA-OAEP +
        /// AES-256-GCM blob — see Common/Resources.cs in the Blazor source). Contains,
        /// among other things, the DES key dashboard_user.password was encrypted with.</summary>
        public string ResourcesPath { get; set; } = string.Empty;

        /// <summary>Path to the RSA private key (PEM) that unwraps ResourcesPath. Never
        /// commit the real file — set both paths via user-secrets/environment variables
        /// and keep the files themselves outside the repo (or gitignored).</summary>
        public string PrivateKeyPath { get; set; } = string.Empty;
    }
}
