using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Attached only to the "Api" named HttpClient (see Program.cs) — the "ApiAnonymous"
    /// client used for login/refresh/forgot-password bypasses this entirely, which is what
    /// avoids a construction cycle with AuthService (resolved lazily here via
    /// IServiceProvider, only when a 401 actually happens, not at handler construction time).
    /// </summary>
    public class AuthorizedHttpMessageHandler : DelegatingHandler
    {
        private readonly TokenStorageService _tokens;
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationManager _navigation;

        public AuthorizedHttpMessageHandler(TokenStorageService tokens, IServiceProvider serviceProvider, NavigationManager navigation)
        {
            _tokens = tokens;
            _serviceProvider = serviceProvider;
            _navigation = navigation;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AttachToken(request);
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();

            var authService = _serviceProvider.GetRequiredService<AuthService>();
            var refreshed = await authService.TryRefreshAsync(cancellationToken);

            if (!refreshed)
            {
                await authService.LogoutLocalAsync();
                // Same returnUrl technique as Shared/RedirectToLogin.razor (the other path
                // that lands the user on /login) so both preserve where they were headed.
                var returnUrl = Uri.EscapeDataString(_navigation.ToBaseRelativePath(_navigation.Uri));
                _navigation.NavigateTo($"/login?reason=session-expired&returnUrl={returnUrl}", forceLoad: false);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var retry = await CloneRequestAsync(request);
            AttachToken(retry);
            return await base.SendAsync(retry, cancellationToken);
        }

        private void AttachToken(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(_tokens.AccessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
        }

        /// <summary>An HttpRequestMessage can only be sent once — this rebuilds an
        /// equivalent one for the post-refresh retry.</summary>
        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            if (request.Content != null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }
    }
}
