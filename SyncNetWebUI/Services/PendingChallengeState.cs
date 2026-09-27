namespace SyncNetWasm.Services
{
    /// <summary>Carries the username + current (temp) password from Login.razor to
    /// ForceChangePassword.razor in memory only — never in the URL (query strings end up in
    /// browser history/referrer headers, which a password must never touch, even a
    /// short-lived temp one).</summary>
    public class PendingChallengeState
    {
        public string? UserName { get; set; }
        public string? CurrentPassword { get; set; }

        public void Clear()
        {
            UserName = null;
            CurrentPassword = null;
        }
    }
}
