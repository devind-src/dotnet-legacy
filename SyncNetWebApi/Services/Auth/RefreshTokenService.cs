using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;

namespace SyncNetApi.Services.Auth
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly SyncNetDbContext _context;

        public RefreshTokenService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task RevokeAllForUserAsync(string userName, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            await _context.RefreshTokens
                .Where(x => x.UserName == userName && x.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, now), ct);
        }
    }
}
