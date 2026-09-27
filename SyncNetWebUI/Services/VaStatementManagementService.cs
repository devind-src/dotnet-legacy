using System.Net.Http.Json;
using SyncNetApi.Dtos.VaTransRequests;

namespace SyncNetWasm.Services
{
    public class VaStatementManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VaStatementManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<VaStatementDto>?> GetStatementAsync(DateTime dateStart, DateTime dateEnd, string? vaName, CancellationToken ct = default)
        {
            // No timezone offset on the wire — sw_trans_pg.time_req is a naive "timestamp
            // without time zone" wall-clock value, so dateStart/dateEnd must travel as plain
            // local wall-clock digits. An "O"-format (with offset) round-trip risks the ASP.NET
            // model binder converting it to Kind=Utc, which Npgsql then refuses to compare
            // against that column type (see VaStatementService.GetStatementAsync).
            const string format = "yyyy-MM-ddTHH:mm:ss";
            var url = $"api/v1/va-statement?dateStart={Uri.EscapeDataString(dateStart.ToString(format))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString(format))}";
            if (!string.IsNullOrWhiteSpace(vaName))
                url += $"&vaName={Uri.EscapeDataString(vaName)}";

            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<VaStatementDto>>(cancellationToken: ct) : null;
        }
    }
}
