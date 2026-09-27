using System.Net.Http.Json;
using SyncNetApi.Dtos.KeyManagement;

namespace SyncNetWasm.Services
{
    /// <summary>Calls the HSM-backed key-generation endpoints (see KeyManagementController —
    /// real DES/3DES math under LMK_1, not a fabricated placeholder) that back the "Generate"
    /// buttons on the Terminal/Merchant/Node Key Management tab.</summary>
    public class KeyManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KeyManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<MasterKeyBundleDto?> GenerateMasterKeyAsync(string keyLength, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/key-management/generate-master-key", new GenerateKeyBundleRequest { KeyLength = keyLength }, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MasterKeyBundleDto>(cancellationToken: ct) : null;
        }

        /// <summary>masterKeyUnderLmk is the Master Key value currently in the form (already
        /// encrypted under LMK_1 by a prior Generate Master Key click) — the session key is
        /// wrapped under it, matching legacy NbHSM.GenerateSessionKey(Key) exactly.</summary>
        public async Task<SessionKeyBundleDto?> GenerateSessionKeyAsync(string masterKeyUnderLmk, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/key-management/generate-session-key", new GenerateSessionKeyRequest { MasterKeyUnderLmk = masterKeyUnderLmk }, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<SessionKeyBundleDto>(cancellationToken: ct) : null;
        }
    }
}
