using System.Net.Http.Json;
using SyncNetApi.Dtos.Lookups;

namespace SyncNetWasm.Services
{
    /// <summary>Read-only dropdown data for tables outside PosBase's own scope (Card &gt;
    /// Group, Configuration &gt; Base &gt; City).</summary>
    public class LookupService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LookupService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<LookupItemDto>?> GetCardGroupsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/card-groups", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetCitiesAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/cities", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetBanksAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/banks", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetBrandsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/brands", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetVaAccountsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/va-accounts", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetMccCodesAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/mcc-codes", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetMerchantCriteriaAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/merchant-criteria", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetProductsAvailableForRoutingAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/products-available-for-routing", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetNonTopupProductsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/products-non-topup", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetTopupProductsAsync(string? category = null, CancellationToken ct = default)
        {
            var url = "api/v1/lookups/products-topup" + (string.IsNullOrWhiteSpace(category) ? "" : $"?category={Uri.EscapeDataString(category)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetTopupCategoriesAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/categories-topup", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetRoutingBillersAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/routing-billers", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetRoutingProductsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/routing-products", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<LookupItemDto>?> GetAppsWithBillerNodeAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/lookups/apps-with-biller-node", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<LookupItemDto>>(cancellationToken: ct) : null;
        }
    }
}
