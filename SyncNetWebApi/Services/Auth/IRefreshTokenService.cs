using System.Threading;
using System.Threading.Tasks;

namespace SyncNetApi.Services.Auth
{
    public interface IRefreshTokenService
    {
        /// <summary>Revokes every currently-active refresh token for a user — called after
        /// any password mutation (self change, self reset-via-token, admin reset, forced
        /// change on login) so old sessions can't silently continue past their access
        /// token's short natural expiry.</summary>
        Task RevokeAllForUserAsync(string userName, CancellationToken ct = default);
    }
}
