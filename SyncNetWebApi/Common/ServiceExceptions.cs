namespace SyncNetApi.Common
{
    /// <summary>Maps to HTTP 404.</summary>
    public class NotFoundException(string message) : Exception(message);

    /// <summary>Maps to HTTP 409.</summary>
    public class ConflictException(string message) : Exception(message);

    /// <summary>Maps to HTTP 400.</summary>
    public class ValidationException(string message) : Exception(message);

    /// <summary>Maps to HTTP 401.</summary>
    public class InvalidCredentialsException(string message) : Exception(message);

    /// <summary>Maps to HTTP 423 (Locked).</summary>
    public class AccountLockedException(string message) : Exception(message);

    /// <summary>Maps to HTTP 503 — HSM math couldn't be performed: no sw_crypto_hsm device is
    /// configured, the selected device's LMK isn't loaded, or the selected device uses the
    /// TCP/IP protocol (real hardware/network HSM integration is out of scope — see
    /// PROJECT_TECHNICAL_SUMMARY.md).</summary>
    public class HsmUnavailableException(string message) : Exception(message);
}
