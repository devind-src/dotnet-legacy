namespace SyncNetWasm.Services
{
    /// <summary>/auth/login (and /auth/login/new-password) can return three different
    /// outcomes with the same 200 status in two of them — this collapses that into one
    /// type the UI can switch on.</summary>
    public class LoginResult
    {
        public bool IsSuccess { get; private init; }
        public bool RequiresNewPassword { get; private init; }
        public string? ChallengeUserName { get; private init; }
        public string? ErrorMessage { get; private init; }

        public static LoginResult Success() => new() { IsSuccess = true };

        public static LoginResult Challenge(string userName) => new()
        {
            RequiresNewPassword = true,
            ChallengeUserName = userName
        };

        public static LoginResult Failed(string error) => new() { ErrorMessage = error };
    }
}
