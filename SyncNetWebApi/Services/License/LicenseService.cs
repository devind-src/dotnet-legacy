using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetApi.Options;

namespace SyncNetApi.Services.License
{
    /// <summary>
    /// Ported from the legacy Blazor app's Common/LicenseManager.cs. Validating means:
    /// decode the configured LicenseKey (see LicenseKeyCodec) and check the embedded serial
    /// matches this machine's disk serial and the embedded expiry is still in the future.
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly LicenseOptions _options;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(IOptions<LicenseOptions> options, ILogger<LicenseService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool Validate()
        {
            var serialNumber = LicenseKeyCodec.ComputeMachineSerialNumber();

            if (string.IsNullOrWhiteSpace(_options.LicenseKey))
                return Fail("License:LicenseKey is not configured.", serialNumber);

            var decoded = LicenseKeyCodec.TryDecrypt(_options.LicenseKey);
            if (decoded == null)
                return Fail("License:LicenseKey could not be decoded (malformed or corrupted).", serialNumber);

            var (embeddedSerial, expiresOn) = decoded.Value;

            if (!string.Equals(embeddedSerial, serialNumber, StringComparison.Ordinal))
                return Fail("License is bound to a different machine.", serialNumber);

            var today = DateOnly.FromDateTime(DateTime.Now);
            if (expiresOn <= today) // legacy semantics: the expiry date itself already counts as expired
                return Fail($"License expired on {expiresOn:yyyy-MM-dd}.", serialNumber);

            _logger.LogInformation("License check passed. Expires {ExpiryDate:yyyy-MM-dd}.", expiresOn);
            return true;
        }

        private bool Fail(string reason, string serialNumber)
        {
            _logger.LogCritical(
                "License validation failed: {Reason} This machine's serial number (for requesting a license key) is {SerialNumber}.",
                reason, serialNumber);
            return false;
        }
    }
}
