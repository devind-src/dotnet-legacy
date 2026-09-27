using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SyncNetApi.Services.Notifications
{
    /// <summary>
    /// Phase-1 stand-in for a real email provider — no SMTP/SendGrid/etc. wiring exists yet.
    /// Logs the link (visible in console/dev logs) and remembers the last one issued per
    /// user so the dev-only endpoint (see Program.cs, Development-only) can hand it back for
    /// testing without scraping logs. Swap for a real IEmailSender implementation later
    /// without touching any caller — same seam as ILicenseService / ILegacyPasswordVerifier.
    /// </summary>
    public class SimulatedEmailSender : IEmailSender
    {
        private readonly ILogger<SimulatedEmailSender> _logger;
        private readonly ConcurrentDictionary<string, string> _lastLinkByUserName = new(StringComparer.OrdinalIgnoreCase);

        public SimulatedEmailSender(ILogger<SimulatedEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendPasswordResetLinkAsync(string toEmail, string userName, string resetLink, CancellationToken ct = default)
        {
            _lastLinkByUserName[userName] = resetLink;
            _logger.LogInformation(
                "[SIMULATED EMAIL] To: {Email} — password reset link for '{UserName}': {Link}",
                toEmail, userName, resetLink);

            return Task.CompletedTask;
        }

        /// <summary>Dev-only escape hatch — never expose this outside an endpoint gated by
        /// IWebHostEnvironment.IsDevelopment().</summary>
        public bool TryGetLastLink(string userName, out string? link) => _lastLinkByUserName.TryGetValue(userName, out link);
    }
}
