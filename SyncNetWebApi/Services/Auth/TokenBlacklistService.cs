using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SyncNetApi.Data;
using SyncNetApi.Entities;

namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Revoked tokens are persisted in Postgres (survives app restarts) and mirrored into
    /// IMemoryCache so the OnTokenValidated check on every request doesn't hit the DB every
    /// single time within this instance's process lifetime.
    ///
    /// Scale-out note: IMemoryCache is per-instance. If you run more than one API instance
    /// behind a load balancer, a token revoked on instance A won't be reflected in instance
    /// B's cache until that row is queried directly — the DB lookup below always runs as a
    /// fallback so correctness is preserved, but for lower latency at scale, swap
    /// IMemoryCache for IDistributedCache backed by Redis.
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly SyncNetDbContext _context;
        private readonly IMemoryCache _cache;

        public TokenBlacklistService(SyncNetDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private static string CacheKey(string jti) => $"blacklist:{jti}";

        public async Task RevokeAsync(string jti, string userName, DateTime tokenExpiresAt, CancellationToken ct = default)
        {
            _context.BlacklistedTokens.Add(new BlacklistedToken
            {
                Jti = jti,
                UserName = userName,
                ExpiresAt = tokenExpiresAt,
                RevokedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);

            var ttl = tokenExpiresAt - DateTime.UtcNow;
            _cache.Set(CacheKey(jti), true, ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1));
        }

        public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey(jti), out bool cached))
                return cached;

            var exists = await _context.BlacklistedTokens.AsNoTracking().AnyAsync(x => x.Jti == jti, ct);

            if (exists)
                _cache.Set(CacheKey(jti), true, TimeSpan.FromMinutes(5));

            return exists;
        }

        public async Task PurgeExpiredAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            await _context.BlacklistedTokens
                .Where(x => x.ExpiresAt < now)
                .ExecuteDeleteAsync(ct);
        }
    }
}
