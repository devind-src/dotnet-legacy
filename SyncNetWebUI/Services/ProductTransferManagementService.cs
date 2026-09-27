using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductTransfers;

namespace SyncNetWasm.Services
{
    public class ProductTransferManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductTransferManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductTransferDto>?> GetTransfersAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/master/transfers" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductTransferDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductTransferDto? Transfer, string? Error)> CreateTransferAsync(CreateProductTransferRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/master/transfers", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductTransferDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTransferAsync(long id, UpdateProductTransferRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/master/transfers/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTransferAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/master/transfers/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
