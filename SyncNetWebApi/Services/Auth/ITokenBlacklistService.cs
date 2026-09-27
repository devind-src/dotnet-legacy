using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNetApi.Services.Auth
{
    public interface ITokenBlacklistService
    {
        Task RevokeAsync(string jti, string userName, DateTime tokenExpiresAt, CancellationToken ct = default);
        Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
        Task PurgeExpiredAsync(CancellationToken ct = default);
    }
}
