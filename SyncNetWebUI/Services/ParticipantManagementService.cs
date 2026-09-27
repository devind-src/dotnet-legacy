using System.Net.Http.Json;
using SyncNetApi.Dtos.Participants;

namespace SyncNetWasm.Services
{
    /// <summary>CRUD against api/v1/participants. Mirrors RoleManagementService's pattern.</summary>
    public class ParticipantManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ParticipantManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ParticipantDto>?> GetParticipantsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/participants" : $"api/v1/participants?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ParticipantDto>>(cancellationToken: ct) : null;
        }

        public async Task<ParticipantDto?> GetParticipantAsync(string participantId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/participants/{Uri.EscapeDataString(participantId)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ParticipantDto>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ParticipantDto? Participant, string? Error)> CreateParticipantAsync(CreateParticipantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/participants", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ParticipantDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateParticipantAsync(string participantId, UpdateParticipantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/participants/{Uri.EscapeDataString(participantId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteParticipantAsync(string participantId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/participants/{Uri.EscapeDataString(participantId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
