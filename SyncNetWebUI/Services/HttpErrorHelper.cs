using System.Net.Http.Json;
using System.Text.Json;

namespace SyncNetWasm.Services
{
    /// <summary>Shared across services calling the API: pulls the "detail" field out of a
    /// failed response's ProblemDetails JSON body, if there is one. Returns null for an
    /// empty body (e.g. a bare 404 NotFound()) or a non-JSON body.</summary>
    internal static class HttpErrorHelper
    {
        public static async Task<string?> TryReadProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (problem.TryGetProperty("detail", out var detail)) return detail.GetString();
            }
            catch (JsonException)
            {
                // Not a JSON/ProblemDetails body — fall through to the generic message.
            }
            return null;
        }
    }
}
