namespace SyncNetApi.Services.Auth
{
    public enum PasswordVerifyResult
    {
        Failed,
        Success,
        SuccessNeedsRehash
    }

    public interface IPasswordService
    {
        /// <summary>Hashes a plain-text password using the modern algorithm. Use for new/changed passwords.</summary>
        string Hash(string plainPassword);

        /// <summary>
        /// Verifies a plain-text password against a stored hash. Transparently supports both
        /// the modern hash format and the legacy DES-encrypted format inherited from the
        /// Blazor app, so existing accounts keep working without a forced reset.
        /// Returns SuccessNeedsRehash when the legacy format matched — the caller should
        /// immediately re-hash and persist the new value (see AuthController.Login).
        /// </summary>
        PasswordVerifyResult Verify(string userName, string plainPassword, string storedHash);
    }
}
