namespace SyncNetApi.Options
{
    public class CaptchaOptions
    {
        public const string SectionName = "Captcha";

        /// <summary>Wajibkan captcha di POST /auth/login. Matikan hanya untuk lingkungan uji.</summary>
        public bool Enabled { get; set; } = true;

        public int Length { get; set; } = 5;

        public int ExpiryMinutes { get; set; } = 2;
    }
}
