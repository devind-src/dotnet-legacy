using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductPostpaidFees;

namespace SyncNetWasm.Services
{
    public class ProductPostpaidFeeManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductPostpaidFeeManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductPostpaidFeeDto>?> GetFeesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/postpaid-fees" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductPostpaidFeeDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductPostpaidFeeDto? Item, string? Error)> CreateFeeAsync(CreateProductPostpaidFeeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/postpaid-fees", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductPostpaidFeeDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateFeeAsync(long id, UpdateProductPostpaidFeeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/postpaid-fees/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteFeeAsync(long id, bool deleteBillers = false, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/postpaid-fees/{id}" + (deleteBillers ? "?deleteBillers=true" : ""), ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
