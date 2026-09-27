namespace SyncNetApi.Dtos.Auth
{
    /// <summary>Returned by /auth/login instead of LoginResponse when the account can't be
    /// fully logged in yet — no access/refresh tokens are issued. Modeled after AWS Cognito's
    /// NEW_PASSWORD_REQUIRED challenge.</summary>
    public class LoginChallengeResponse
    {
        public const string NewPasswordRequired = "NEW_PASSWORD_REQUIRED";

        public string ChallengeRequired { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
