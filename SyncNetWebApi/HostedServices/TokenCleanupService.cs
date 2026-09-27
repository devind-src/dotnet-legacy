using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncNetApi.Data;
using SyncNetApi.Services.Auth;

namespace SyncNetApi.HostedServices
{
    /// <summary>
    /// Runs hourly to purge blacklist rows and expired/revoked refresh tokens that no
    /// longer matter, keeping api_token_blacklist / api_refresh_token from growing forever.
    /// </summary>
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TokenCleanupService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var blacklist = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
                    var context = scope.ServiceProvider.GetRequiredService<SyncNetDbContext>();

                    await blacklist.PurgeExpiredAsync(stoppingToken);

                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    await context.RefreshTokens
                        .Where(x => x.ExpiresAt < cutoff)
                        .ExecuteDeleteAsync(stoppingToken);

                    _logger.LogDebug("Token cleanup pass completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token cleanup pass failed.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
    }
}
