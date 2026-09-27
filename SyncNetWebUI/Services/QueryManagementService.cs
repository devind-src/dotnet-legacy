using System.Net.Http.Json;
using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetWasm.Services
{
    /// <summary>Backs Queries &gt; Transaction/Audit Trail/User Log (§7.27). All three search
    /// endpoints are server-paginated (see PagedResultDto) — the usual "load everything, page
    /// client-side" pattern used by most list pages in this app doesn't fit here (sw_audit/
    /// dashboard_user_log already have hundreds of rows from dev testing alone).</summary>
    public class QueryManagementService
    {
        // sw_trans_pg.time_req / sw_audit.updatedate / dashboard_user_log.date_time are all
        // "timestamp without time zone" — sending a plain wall-clock string with no offset
        // avoids the Kind mismatch documented for VA Statement (§7.21) and Queries (§7.27).
        private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

        private readonly IHttpClientFactory _httpClientFactory;

        public QueryManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<PagedResultDto<TransactionListItemDto>?> SearchTransactionsAsync(TransactionSearchRequest request, CancellationToken ct = default)
        {
            var qs = new List<string>();
            AddDateRange(qs, request.DateStart, request.DateEnd);
            AddIfNotEmpty(qs, "sourceNode", request.SourceNode);
            AddIfNotEmpty(qs, "destNode", request.DestNode);
            AddIfNotEmpty(qs, "merchantId", request.MerchantId);
            AddIfNotEmpty(qs, "terminalId", request.TerminalId);
            AddIfNotEmpty(qs, "accountNo", request.AccountNo);
            AddIfNotEmpty(qs, "respCode", request.RespCode);
            AddIfNotEmpty(qs, "traceNumber", request.TraceNumber);
            AddIfNotEmpty(qs, "refNumber", request.RefNumber);
            qs.Add($"pageNumber={request.PageNumber}");
            qs.Add($"pageSize={request.PageSize}");

            var response = await Client.GetAsync($"api/v1/queries/transactions?{string.Join('&', qs)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionListItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<TransactionDetailDto?> GetTransactionAsync(long tranNr, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/queries/transactions/{tranNr}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<TransactionDetailDto>(cancellationToken: ct) : null;
        }

        public async Task<PagedResultDto<AuditListItemDto>?> SearchAuditsAsync(AuditSearchRequest request, CancellationToken ct = default)
        {
            var qs = new List<string>();
            AddDateRange(qs, request.DateStart, request.DateEnd);
            AddIfNotEmpty(qs, "tableName", request.TableName);
            AddIfNotEmpty(qs, "userName", request.UserName);
            qs.Add($"pageNumber={request.PageNumber}");
            qs.Add($"pageSize={request.PageSize}");

            var response = await Client.GetAsync($"api/v1/queries/audits?{string.Join('&', qs)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagedResultDto<AuditListItemDto>>(cancellationToken: ct) : null;
        }

        public async Task<AuditDetailDto?> GetAuditAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/queries/audits/{id}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AuditDetailDto>(cancellationToken: ct) : null;
        }

        public async Task<PagedResultDto<UserLogItemDto>?> SearchUserLogsAsync(UserLogSearchRequest request, CancellationToken ct = default)
        {
            var qs = new List<string>();
            AddDateRange(qs, request.DateStart, request.DateEnd);
            AddIfNotEmpty(qs, "userName", request.UserName);
            AddIfNotEmpty(qs, "state", request.State);
            qs.Add($"pageNumber={request.PageNumber}");
            qs.Add($"pageSize={request.PageSize}");

            var response = await Client.GetAsync($"api/v1/queries/user-logs?{string.Join('&', qs)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagedResultDto<UserLogItemDto>>(cancellationToken: ct) : null;
        }

        private static void AddDateRange(List<string> qs, DateTime? dateStart, DateTime? dateEnd)
        {
            if (dateStart.HasValue) qs.Add($"dateStart={Uri.EscapeDataString(dateStart.Value.ToString(DateFormat))}");
            if (dateEnd.HasValue) qs.Add($"dateEnd={Uri.EscapeDataString(dateEnd.Value.ToString(DateFormat))}");
        }

        private static void AddIfNotEmpty(List<string> qs, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) qs.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }
}
