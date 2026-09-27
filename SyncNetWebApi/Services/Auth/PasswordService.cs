using Microsoft.AspNetCore.Identity;

namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Uses ASP.NET Core Identity's PasswordHasher&lt;T&gt; — no external dependency needed.
    /// It currently implements PBKDF2-HMAC-SHA256 with 100,000+ iterations and a versioned
    /// output format, so the algorithm can be upgraded later without breaking old hashes.
    /// This replaces the legacy reversible DES scheme (Library/NbCreden.Encrypt), which
    /// should be considered a critical finding to remediate — DES is both obsolete (56-bit
    /// key) and, worse, reversible: anyone with the key can recover the plain-text password.
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<object> _hasher = new();
        private readonly ILegacyPasswordVerifier _legacyVerifier;

        public PasswordService(ILegacyPasswordVerifier legacyVerifier)
        {
            _legacyVerifier = legacyVerifier;
        }

        public string Hash(string plainPassword) => _hasher.HashPassword(new object(), plainPassword);

        public PasswordVerifyResult Verify(string userName, string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return PasswordVerifyResult.Failed;

            // NOTE: legacy DES values are ALSO valid base64 (NbCreden.Encrypt ends with
            // Convert.ToBase64String) — VerifyHashedPassword decodes them fine and just
            // returns Failed for an unrecognized format marker byte, it does NOT throw.
            // So a caught FormatException alone can't be used to detect "not a modern
            // hash": that only fires for a storedHash that isn't even valid base64 (wrong
            // length/characters), which legacy values never are. Every non-Success modern
            // result — thrown or not — falls through to the legacy check below.
            try
            {
                var result = _hasher.VerifyHashedPassword(new object(), storedHash, plainPassword);
                switch (result)
                {
                    case PasswordVerificationResult.Success:
                        return PasswordVerifyResult.Success;
                    case PasswordVerificationResult.SuccessRehashNeeded:
                        return PasswordVerifyResult.SuccessNeedsRehash;
                }
            }
            catch (FormatException)
            {
                // storedHash isn't valid base64 at all — fall through to the legacy check.
            }

            var legacyMatch = _legacyVerifier.Matches(userName, plainPassword, storedHash);
            return legacyMatch ? PasswordVerifyResult.SuccessNeedsRehash : PasswordVerifyResult.Failed;
        }
    }
}
