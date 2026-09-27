using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Entities;
using SyncNetApi.Options;
using SyncNetApi.Services.Notifications;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Services.Auth
{
    public class PasswordResetService : IPasswordResetService
    {
        // Below this, a repeat request for the same account is dropped rather than issuing
        // (and emailing) a fresh token — keeps someone from mailbox-bombing another user via
        // this endpoint even from a rotating set of IPs the per-IP rate limiter wouldn't catch.
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        private readonly SyncNetDbContext _context;
        private readonly IUserService _users;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IEmailSender _emailSender;
        private readonly PasswordResetOptions _options;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            SyncNetDbContext context,
            IUserService users,
            IRefreshTokenService refreshTokens,
            IEmailSender emailSender,
            IOptions<PasswordResetOptions> options,
            ILogger<PasswordResetService> logger)
        {
            _context = context;
            _users = users;
            _refreshTokens = refreshTokens;
            _emailSender = emailSender;
            _options = options.Value;
            _logger = logger;
        }

        public async Task RequestResetAsync(string identifier, string? requestIp, CancellationToken ct = default)
        {
            var user = await _users.GetEntityByUserNameAsync(identifier)
                ?? await _users.GetEntityByEmailAsync(identifier);

            // Same non-committal outcome whether the account doesn't exist, is disabled, or
            // has no email on file — never let this endpoint confirm/deny any of that.
            if (user == null || user.status != "1" || string.IsNullOrWhiteSpace(user.email))
            {
                _logger.LogInformation("Password reset requested for unresolvable identifier (no-op).");
                return;
            }

            var recentPending = await _context.UserReset
                .Where(x => x.user_name == user.user_name && x.status == "0")
                .OrderByDescending(x => x.time_request)
                .FirstOrDefaultAsync(ct);

            if (recentPending != null && DateTime.UtcNow - recentPending.time_request < ResendCooldown)
            {
                _logger.LogInformation("Password reset requested again too soon for '{UserName}' — skipping.", user.user_name);
                return;
            }

            // Only one usable token per account at a time — supersede whatever's pending.
            var stillPending = await _context.UserReset
                .Where(x => x.user_name == user.user_name && x.status == "0")
                .ToListAsync(ct);
            foreach (var old in stillPending) old.status = "1";

            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashToken(rawToken);

            _context.UserReset.Add(new DashboardUserReset
            {
                user_name = user.user_name,
                email = user.email,
                vcode = tokenHash,
                time_request = DateTime.UtcNow,
                status = "0",
                requested_ip = requestIp
            });

            await _context.SaveChangesAsync(ct);

            var resetLink = $"{_options.ResetLinkBaseUrl}?token={rawToken}";
            await _emailSender.SendPasswordResetLinkAsync(user.email, user.user_name, resetLink, ct);
        }

        public async Task CompleteResetAsync(string rawToken, string newPassword, CancellationToken ct = default)
        {
            var tokenHash = HashToken(rawToken);
            var row = await _context.UserReset.FirstOrDefaultAsync(x => x.vcode == tokenHash && x.status == "0", ct);

            var expiry = TimeSpan.FromMinutes(_options.TokenExpiryMinutes);
            var isExpired = row != null && DateTime.UtcNow - row.time_request > expiry;

            if (row == null || string.IsNullOrEmpty(row.user_name) || isExpired)
            {
                if (row != null)
                {
                    // Expired but presented once — burn it so it can't be tried again later.
                    row.status = "1";
                    await _context.SaveChangesAsync(ct);
                }

                throw new ValidationException("Invalid or expired reset token.");
            }

            row.status = "1";
            row.consumed_at = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await _users.ResetPasswordAsync(row.user_name, newPassword, actingUser: row.user_name, mustChangePassword: false);
            await _refreshTokens.RevokeAllForUserAsync(row.user_name, ct);
        }

        private static string HashToken(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
