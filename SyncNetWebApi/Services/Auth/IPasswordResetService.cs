using System.Threading;
using System.Threading.Tasks;

namespace SyncNetApi.Services.Auth
{
    public interface IPasswordResetService
    {
        /// <summary>Resolves <paramref name="identifier"/> as either a user_name or an email
        /// and, if it matches exactly one active account, issues a reset token and emails
        /// (simulated) a link. Never throws and never reveals whether a match was found —
        /// callers must always show the same generic response either way.</summary>
        Task RequestResetAsync(string identifier, string? requestIp, CancellationToken ct = default);

        /// <summary>Validates and consumes a reset token, sets the new password, and revokes
        /// every active session for that account. Throws ValidationException (-&gt; 400) if the
        /// token is missing, already used, or expired.</summary>
        Task CompleteResetAsync(string rawToken, string newPassword, CancellationToken ct = default);
    }
}
