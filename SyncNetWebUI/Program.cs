using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ApexCharts;
using Blazored.LocalStorage;
using MudBlazor.Services;
using SyncNetWasm;
using SyncNetWasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiOptions = builder.Configuration.GetSection("Api").Get<ApiOptions>() ?? new ApiOptions();
builder.Services.AddSingleton(apiOptions);

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();

// Secure by default: every page requires auth unless explicitly marked [AllowAnonymous]
// (Login, ForgotPassword, ResetPassword, ForceChangePassword) — appropriate for an
// internal admin tool where almost everything is behind a login.
builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ThemeState>();

// Own-origin client for static assets like wwwroot/version.json — separate from "Api"/
// "ApiAnonymous" above, which point at the external SyncNetApi base URL.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AppVersionCheckService>();
builder.Services.AddScoped<PendingChallengeState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IdleTimerService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<RoleManagementService>();
builder.Services.AddScoped<MenuManagementService>();
builder.Services.AddScoped<ParticipantManagementService>();
builder.Services.AddScoped<MerchantManagementService>();
builder.Services.AddScoped<MerchantGroupManagementService>();
builder.Services.AddScoped<SubMerchantGroupManagementService>();
builder.Services.AddScoped<SubMerchantManagementService>();
builder.Services.AddScoped<StoreManagementService>();
builder.Services.AddScoped<TerminalManagementService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<KeyManagementService>();
builder.Services.AddScoped<PosnetBinManagementService>();
builder.Services.AddScoped<TerminalLimitManagementService>();
builder.Services.AddScoped<TerminalClientManagementService>();
builder.Services.AddScoped<TerminalImageManagementService>();
builder.Services.AddScoped<SoundBoxManagementService>();
builder.Services.AddScoped<AccountTypeManagementService>();
builder.Services.AddScoped<CountryManagementService>();
builder.Services.AddScoped<CurrencyManagementService>();
builder.Services.AddScoped<TranNameManagementService>();
builder.Services.AddScoped<CityManagementService>();
builder.Services.AddScoped<ProvinceManagementService>();
builder.Services.AddScoped<BankManagementService>();
builder.Services.AddScoped<BrandManagementService>();
builder.Services.AddScoped<MccManagementService>();
builder.Services.AddScoped<ProductBinManagementService>();
builder.Services.AddScoped<SupportTeamManagementService>();
builder.Services.AddScoped<SupportMemberManagementService>();
builder.Services.AddScoped<BusinessDateManagementService>();
builder.Services.AddScoped<PublicHolidayManagementService>();
builder.Services.AddScoped<JobScheduleManagementService>();
builder.Services.AddScoped<JobCleanerManagementService>();
builder.Services.AddScoped<ProductManagementService>();
builder.Services.AddScoped<JobFeeManagementService>();
builder.Services.AddScoped<AppManagementService>();
builder.Services.AddScoped<NodeManagementService>();
builder.Services.AddScoped<ConnectionManagementService>();
builder.Services.AddScoped<RouteSourceManagementService>();
builder.Services.AddScoped<RouteBinManagementService>();
builder.Services.AddScoped<RouteProductManagementService>();
builder.Services.AddScoped<RouteProductAltManagementService>();
builder.Services.AddScoped<RouteMarginManagementService>();
builder.Services.AddScoped<RouteFailoverConfigManagementService>();
builder.Services.AddScoped<RouteScheduleManagementService>();
builder.Services.AddScoped<RouteSupplierStatusManagementService>();
builder.Services.AddScoped<RouteFailoverLogManagementService>();
builder.Services.AddScoped<ProductCategoryManagementService>();
builder.Services.AddScoped<ProductMappingManagementService>();
builder.Services.AddScoped<ProductTransferManagementService>();
builder.Services.AddScoped<ProductMerchantManagementService>();
builder.Services.AddScoped<ProductPrepaidPricingManagementService>();
builder.Services.AddScoped<ProductMerchantPricingManagementService>();
builder.Services.AddScoped<MerchantCriteriaManagementService>();
builder.Services.AddScoped<ProductPostpaidFeeManagementService>();
builder.Services.AddScoped<ProductPostpaidBillerManagementService>();
builder.Services.AddScoped<RoutingManagementService>();
builder.Services.AddScoped<ProductAdditionalFeeManagementService>();
builder.Services.AddScoped<ProductPromotionManagementService>();
builder.Services.AddScoped<ProductVolumeTieringManagementService>();
builder.Services.AddScoped<CardGroupManagementService>();
builder.Services.AddScoped<CardBinManagementService>();
builder.Services.AddScoped<CardAccountManagementService>();
builder.Services.AddScoped<CardHotcardManagementService>();
builder.Services.AddScoped<CardIssuerManagementService>();
builder.Services.AddScoped<CardBranchManagementService>();
builder.Services.AddScoped<CardProductManagementService>();
builder.Services.AddScoped<CardProductLimitManagementService>();
builder.Services.AddScoped<CardOverrideLimitManagementService>();
builder.Services.AddScoped<VaGroupManagementService>();
builder.Services.AddScoped<VaAccountManagementService>();
builder.Services.AddScoped<VaTransRequestManagementService>();
builder.Services.AddScoped<VaStatementManagementService>();
builder.Services.AddScoped<CashoutWithdrawalManagementService>();
builder.Services.AddScoped<CashoutMiniAtmManagementService>();
builder.Services.AddScoped<AgentBankManagementService>();
builder.Services.AddScoped<MonitoringManagementService>();
builder.Services.AddScoped<QueryManagementService>();
builder.Services.AddScoped<HsmDeviceManagementService>();
builder.Services.AddScoped<HsmServiceManagementService>();
builder.Services.AddScoped<HsmConsoleService>();
builder.Services.AddScoped<ToolsService>();
builder.Services.AddTransient<AuthorizedHttpMessageHandler>();

// Bounded so a down/unreachable API fails fast with a clear error instead of leaving the
// caller (e.g. the login button's spinner) stuck for the default 100s HttpClient timeout.
var apiCallTimeout = TimeSpan.FromSeconds(20);

// Unauthenticated: login, refresh, forgot/reset-password — never carries a bearer token,
// which is what lets AuthService use it during the refresh-on-401 flow without recursing
// back into AuthorizedHttpMessageHandler.
builder.Services.AddHttpClient("ApiAnonymous", client =>
{
    client.BaseAddress = new Uri(apiOptions.BaseUrl);
    client.Timeout = apiCallTimeout;
});

// Authenticated: everything else. Bearer token attached, 401 triggers one silent
// refresh-and-retry (see AuthorizedHttpMessageHandler). The standard resilience handler
// (retry w/ backoff + circuit breaker, all on transient failures — 5xx/408/network errors,
// never on 401) sits inside the auth handler so a hiccup mid-request (e.g. the API restarting
// during a routine deploy) is retried transparently instead of surfacing as Blazor's generic
// "unhandled error, reload" screen (this project has no <ErrorBoundary>). Login/refresh use
// "ApiAnonymous" below and deliberately don't get this — retrying a login POST automatically
// isn't something we want, and AuthService already handles that client's connectivity
// failures explicitly (IsConnectivityException) with a clear user-facing message.
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiOptions.BaseUrl);
    client.Timeout = apiCallTimeout;
})
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Each attempt still has to fit inside apiCallTimeout's 20s total budget, so the
        // per-try/overall timeouts are tightened from the library's defaults (10s/30s) —
        // otherwise a single retried attempt could exceed what the rest of the app assumes
        // "a call" costs.
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
        options.TotalRequestTimeout.Timeout = apiCallTimeout;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20); // must exceed 2x AttemptTimeout, per Polly's requirement
    });

var host = builder.Build();

// Silent refresh from any refresh token already in localStorage, so a page reload doesn't
// bounce a still-valid session back to the login page.
await host.Services.GetRequiredService<AuthService>().InitializeAsync();

await host.RunAsync();
