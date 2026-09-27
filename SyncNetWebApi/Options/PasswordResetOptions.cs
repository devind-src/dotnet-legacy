namespace SyncNetApi.Options
{
    public class PasswordResetOptions
    {
        public const string SectionName = "PasswordReset";

        public int TokenExpiryMinutes { get; set; } = 30;

        /// <summary>Base URL of the frontend's reset-password page — the emailed link is
        /// "{ResetLinkBaseUrl}?token={rawToken}". This API has no UI of its own.</summary>
        public string ResetLinkBaseUrl { get; set; } = string.Empty;
    }
}
