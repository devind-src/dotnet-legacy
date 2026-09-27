namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Bridges to the legacy password scheme used by the Blazor app
    /// (Library/NbCreden.Encrypt, DES-based, keyed from the RSA-protected Resources.bin).
    /// This is isolated behind its own interface so it can be deleted entirely once every
    /// existing user has logged in at least once post-migration and been auto-upgraded to
    /// the modern hash (see PasswordService.Verify).
    /// </summary>
    public interface ILegacyPasswordVerifier
    {
        bool Matches(string userName, string plainPassword, string storedValue);
    }

    /// <summary>
    /// Fallback when LegacyCredentials isn't configured or Resources.bin can't be loaded —
    /// always returns false, meaning legacy accounts can't log in until that's fixed. The
    /// real implementation is <see cref="LegacyPasswordVerifier"/>; this one exists so the
    /// app still starts and modern (non-legacy) accounts still work without it.
    /// </summary>
    public class NotImplementedLegacyPasswordVerifier : ILegacyPasswordVerifier
    {
        public bool Matches(string userName, string plainPassword, string storedValue) => false;
    }
}
