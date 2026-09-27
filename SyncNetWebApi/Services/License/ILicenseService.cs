namespace SyncNetApi.Services.License
{
    public interface ILicenseService
    {
        /// <summary>
        /// Validates the license once. Intended to be called a single time during
        /// application startup (see Program.cs) — NOT per-request, unlike the old
        /// Blazor app which re-checked on every login.
        /// </summary>
        bool Validate();
    }
}
