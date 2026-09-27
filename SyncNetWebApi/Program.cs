using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SyncNetApi.Cryptography;
using SyncNetApi.Data;
using SyncNetApi.HostedServices;
using SyncNetApi.Logging;
using SyncNetApi.Options;
using SyncNetApi.Services.AccountTypes;
using SyncNetApi.Services.AgentBanks;
using SyncNetApi.Services.Apps;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.Auth;
using SyncNetApi.Services.Banks;
using SyncNetApi.Services.Brands;
using SyncNetApi.Services.BusinessDates;
using SyncNetApi.Services.CardAccounts;
using SyncNetApi.Services.CardBins;
using SyncNetApi.Services.CardBranches;
using SyncNetApi.Services.CardGroups;
using SyncNetApi.Services.CardHotcards;
using SyncNetApi.Services.CardIssuers;
using SyncNetApi.Services.CardOverrideLimits;
using SyncNetApi.Services.CardProductLimits;
using SyncNetApi.Services.CardProducts;
using SyncNetApi.Services.CashoutMiniAtms;
using SyncNetApi.Services.CashoutWithdrawals;
using SyncNetApi.Services.Cities;
using SyncNetApi.Services.Connections;
using SyncNetApi.Services.Countries;
using SyncNetApi.Services.Currencies;
using SyncNetApi.Services.Hsm;
using SyncNetApi.Services.HsmDevices;
using SyncNetApi.Services.HsmServices;
using SyncNetApi.Services.JobCleaners;
using SyncNetApi.Services.JobFees;
using SyncNetApi.Services.JobSchedules;
using SyncNetApi.Services.License;
using SyncNetApi.Services.Menus;
using SyncNetApi.Services.MerchantCriteria;
using SyncNetApi.Services.Mccs;
using SyncNetApi.Services.MerchantGroups;
using SyncNetApi.Services.Merchants;
using SyncNetApi.Services.Monitoring;
using SyncNetApi.Services.Nodes;
using SyncNetApi.Services.Notifications;
using SyncNetApi.Services.Participants;
using SyncNetApi.Services.PosnetBins;
using SyncNetApi.Services.ProductBins;
using SyncNetApi.Services.Products;
using SyncNetApi.Services.Provinces;
using SyncNetApi.Services.ProductCategories;
using SyncNetApi.Services.ProductAdditionalFees;
using SyncNetApi.Services.ProductPostpaidFees;
using SyncNetApi.Services.ProductVolumeTierings;
using SyncNetApi.Services.ProductMappings;
using SyncNetApi.Services.ProductMerchantPricing;
using SyncNetApi.Services.ProductMerchants;
using SyncNetApi.Services.ProductPromotions;
using SyncNetApi.Services.ProductPrepaidPricing;
using SyncNetApi.Services.ProductTransfers;
using SyncNetApi.Services.PublicHolidays;
using SyncNetApi.Services.Queries;
using SyncNetApi.Services.RouteBins;
using SyncNetApi.Services.RouteFailoverConfigs;
using SyncNetApi.Services.RouteFailoverLogs;
using SyncNetApi.Services.RouteMargins;
using SyncNetApi.Services.RouteProductAlts;
using SyncNetApi.Services.RouteProducts;
using SyncNetApi.Services.RouteSources;
using SyncNetApi.Services.RouteSupplierStatuses;
using SyncNetApi.Services.Roles;
using SyncNetApi.Services.SupportMembers;
using SyncNetApi.Services.SupportTeams;
using SyncNetApi.Services.SoundBoxes;
using SyncNetApi.Services.SubMerchantGroups;
using SyncNetApi.Services.Stores;
using SyncNetApi.Services.SubMerchants;
using SyncNetApi.Services.Terminals;
using SyncNetApi.Services.TerminalClients;
using SyncNetApi.Services.TerminalImages;
using SyncNetApi.Services.TerminalLimits;
using SyncNetApi.Services.Tools;
using SyncNetApi.Services.TranNames;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaAccounts;
using SyncNetApi.Services.VaGroups;
using SyncNetApi.Services.VaStatements;
using SyncNetApi.Services.VaTransRequests;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// File logging — captures Error+ entries (including every unhandled exception logged by
// UseExceptionHandler() below) to disk so a deployment can be diagnosed without console
// access. Folder/FileName/MinLevel are configured per environment in appsettings.json under
// "Logging:File", not hardcoded here.
// ---------------------------------------------------------------------------
var fileLoggingOptions = builder.Configuration.GetSection(FileLoggingOptions.SectionName).Get<FileLoggingOptions>()
    ?? new FileLoggingOptions();
builder.Logging.AddProvider(new FileLoggerProvider(fileLoggingOptions));

// ---------------------------------------------------------------------------
// Configuration binding
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection(LicenseOptions.SectionName));
builder.Services.Configure<LegacyCredentialsOptions>(builder.Configuration.GetSection(LegacyCredentialsOptions.SectionName));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection(PasswordResetOptions.SectionName));
builder.Services.Configure<TerminalImageStorageOptions>(builder.Configuration.GetSection(TerminalImageStorageOptions.SectionName));
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection(MonitoringOptions.SectionName));
builder.Services.Configure<CaptchaOptions>(builder.Configuration.GetSection(CaptchaOptions.SectionName));

// ---------------------------------------------------------------------------
// Legacy resources (Resources.bin) — built manually here, before builder.Build(), because
// the DB connection string below may be encrypted under its AES.Config key. Registered as
// the same instance into DI further down (see "Application services") so Resources.bin is
// only decrypted once at boot, not again the first time something resolves
// LegacyResourcesLoader through the container.
//
// NOTE: this makes Resources.bin/PrivateKey.pem a hard startup dependency whenever
// ConnectionStrings:DbConnectionEncrypted is set — unlike LegacyResourcesLoader's own
// "best-effort, must not block startup" contract (see its class doc), which was written
// for its original use (legacy login/HSM emulator only). If DbConnectionEncrypted isn't
// configured, this stays fully optional as before.
// ---------------------------------------------------------------------------
var legacyCredentialsOptions = builder.Configuration.GetSection(LegacyCredentialsOptions.SectionName).Get<LegacyCredentialsOptions>()
    ?? new LegacyCredentialsOptions();
LegacyResourcesLoader legacyResourcesLoader;
using (var bootstrapLoggerFactory = LoggerFactory.Create(b => b.AddConsole()))
{
    legacyResourcesLoader = new LegacyResourcesLoader(
        Options.Create(legacyCredentialsOptions),
        bootstrapLoggerFactory.CreateLogger<LegacyResourcesLoader>());
}

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------
var encryptedConnectionString = builder.Configuration.GetConnectionString("DbConnectionEncrypted");
var connectionString = !string.IsNullOrWhiteSpace(encryptedConnectionString)
    ? AesConfigCipher.Decrypt(
        encryptedConnectionString,
        legacyResourcesLoader.GetAesConfigKeyHex()
            ?? throw new InvalidOperationException("Resources.bin's AES.Config key is not loaded — cannot decrypt ConnectionStrings:DbConnectionEncrypted."),
        legacyResourcesLoader.GetAesConfigIvHex()
            ?? throw new InvalidOperationException("Resources.bin's AES.Config IV is not loaded — cannot decrypt ConnectionStrings:DbConnectionEncrypted."))
    : builder.Configuration.GetConnectionString("DbConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DbConnection is not configured.");

builder.Services.AddDbContext<SyncNetDbContext>(opt => opt.UseNpgsql(connectionString));

// Jwt:SigningKeyEncrypted — same AES.Config cipher as DbConnectionEncrypted above. When set,
// the decrypted value is layered over Jwt:SigningKey so both the JwtOptions binding (used by
// JwtTokenService) and the JwtBearer validation setup below see the plain key.
var encryptedJwtSigningKey = builder.Configuration["Jwt:SigningKeyEncrypted"];
if (!string.IsNullOrWhiteSpace(encryptedJwtSigningKey))
{
    var jwtSigningKey = AesConfigCipher.Decrypt(
        encryptedJwtSigningKey,
        legacyResourcesLoader.GetAesConfigKeyHex()
            ?? throw new InvalidOperationException("Resources.bin's AES.Config key is not loaded — cannot decrypt Jwt:SigningKeyEncrypted."),
        legacyResourcesLoader.GetAesConfigIvHex()
            ?? throw new InvalidOperationException("Resources.bin's AES.Config IV is not loaded — cannot decrypt Jwt:SigningKeyEncrypted."));
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = jwtSigningKey });
}

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IParticipantService, ParticipantService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IMerchantGroupService, MerchantGroupService>();
builder.Services.AddScoped<ISubMerchantGroupService, SubMerchantGroupService>();
builder.Services.AddScoped<ISubMerchantService, SubMerchantService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ITerminalService, TerminalService>();
builder.Services.AddScoped<IPosnetBinService, PosnetBinService>();
builder.Services.AddScoped<ITerminalLimitService, TerminalLimitService>();
builder.Services.AddScoped<ITerminalClientService, TerminalClientService>();
builder.Services.AddScoped<ITerminalImageService, TerminalImageService>();
builder.Services.AddScoped<ISoundBoxService, SoundBoxService>();
builder.Services.AddScoped<IAccountTypeService, AccountTypeService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<ITranNameService, TranNameService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IMccService, MccService>();
builder.Services.AddScoped<IProductBinService, ProductBinService>();
builder.Services.AddScoped<ISupportTeamService, SupportTeamService>();
builder.Services.AddScoped<ISupportMemberService, SupportMemberService>();
builder.Services.AddScoped<IBusinessDateService, BusinessDateService>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<IJobScheduleService, JobScheduleService>();
builder.Services.AddScoped<IJobCleanerService, JobCleanerService>();
builder.Services.AddScoped<IHsmDeviceService, HsmDeviceService>();
builder.Services.AddScoped<IHsmServiceService, HsmServiceService>();
// Singleton: only depends on LegacyResourcesLoader (also singleton) for LMK_1 — no per-request
// state, same reasoning as that loader itself.
builder.Services.AddSingleton<IHsmCryptoProvider, HsmCryptoProvider>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IJobFeeService, JobFeeService>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<INodeService, NodeService>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IRouteSourceService, RouteSourceService>();
builder.Services.AddScoped<IRouteBinService, RouteBinService>();
builder.Services.AddScoped<IRouteProductService, RouteProductService>();
builder.Services.AddScoped<IRouteProductAltService, RouteProductAltService>();
builder.Services.AddScoped<IRouteMarginService, RouteMarginService>();
builder.Services.AddScoped<IRouteFailoverConfigService, RouteFailoverConfigService>();
builder.Services.AddScoped<IRouteSupplierStatusService, RouteSupplierStatusService>();
builder.Services.AddScoped<IRouteFailoverLogService, RouteFailoverLogService>();
builder.Services.AddScoped<SyncNetApi.Services.RouteSchedules.IRouteScheduleService, SyncNetApi.Services.RouteSchedules.RouteScheduleService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductMappingService, ProductMappingService>();
builder.Services.AddScoped<IProductTransferService, ProductTransferService>();
builder.Services.AddScoped<IProductMerchantService, ProductMerchantService>();
builder.Services.AddScoped<IProductPrepaidPricingService, ProductPrepaidPricingService>();
builder.Services.AddScoped<IProductMerchantPricingService, ProductMerchantPricingService>();
builder.Services.AddScoped<IMerchantCriteriaService, MerchantCriteriaService>();
builder.Services.AddScoped<IProductPostpaidFeeService, ProductPostpaidFeeService>();
builder.Services.AddScoped<SyncNetApi.Services.ProductPostpaidBillers.IProductPostpaidBillerService, SyncNetApi.Services.ProductPostpaidBillers.ProductPostpaidBillerService>();
builder.Services.AddScoped<SyncNetApi.Services.Routing.IRoutingApplyService, SyncNetApi.Services.Routing.RoutingApplyService>();
builder.Services.AddScoped<IProductAdditionalFeeService, ProductAdditionalFeeService>();
builder.Services.AddScoped<IProductPromotionService, ProductPromotionService>();
builder.Services.AddScoped<IProductVolumeTieringService, ProductVolumeTieringService>();
builder.Services.AddScoped<ICardGroupService, CardGroupService>();
builder.Services.AddScoped<ICardBinService, CardBinService>();
builder.Services.AddScoped<ICardAccountService, CardAccountService>();
builder.Services.AddScoped<ICardHotcardService, CardHotcardService>();
builder.Services.AddScoped<ICardIssuerService, CardIssuerService>();
builder.Services.AddScoped<ICardBranchService, CardBranchService>();
builder.Services.AddScoped<ICardProductService, CardProductService>();
builder.Services.AddScoped<ICardProductLimitService, CardProductLimitService>();
builder.Services.AddScoped<ICardOverrideLimitService, CardOverrideLimitService>();
builder.Services.AddScoped<ICashoutWithdrawalService, CashoutWithdrawalService>();
builder.Services.AddScoped<ICashoutMiniAtmService, CashoutMiniAtmService>();
builder.Services.AddScoped<IAgentBankService, AgentBankService>();
builder.Services.AddScoped<IVaGroupService, VaGroupService>();
builder.Services.AddScoped<IVaAccountService, VaAccountService>();
builder.Services.AddScoped<IVaTransRequestService, VaTransRequestService>();
builder.Services.AddScoped<IVaStatementService, VaStatementService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IMonitoringLogService, MonitoringLogService>();
builder.Services.AddScoped<ISwitchCommandClient, SwitchCommandClient>();
builder.Services.AddScoped<IMonitoringCommandService, MonitoringCommandService>();
builder.Services.AddScoped<IQueryTransactionService, QueryTransactionService>();
builder.Services.AddScoped<IQueryAuditService, QueryAuditService>();
builder.Services.AddScoped<IQueryUserLogService, QueryUserLogService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IToolsService, ToolsService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddSingleton(legacyResourcesLoader);
builder.Services.AddScoped<ILegacyPasswordVerifier, LegacyPasswordVerifier>();
builder.Services.AddSingleton<ILicenseService, LicenseService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Singleton (not scoped): SimulatedEmailSender holds an in-memory "last link per user"
// map that must survive across requests for the dev-only retrieval endpoint below to work.
// Swap for a real provider (SMTP/SendGrid/etc.) behind the same IEmailSender seam later —
// no other code needs to change.
builder.Services.AddSingleton<SimulatedEmailSender>();
builder.Services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<SimulatedEmailSender>());

builder.Services.AddHostedService<TokenCleanupService>();

// ---------------------------------------------------------------------------
// JWT authentication + blacklist enforcement
// ---------------------------------------------------------------------------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            // Runs after signature/expiry validation succeeds — this is where a
            // logged-out-but-not-yet-expired access token gets rejected.
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst("jti")?.Value;
                if (string.IsNullOrEmpty(jti))
                {
                    context.Fail("Token missing jti claim.");
                    return;
                }

                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                if (await blacklist.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Rate limiting (native .NET) — protects the login endpoint from brute force
// on top of the existing per-account retry lock.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // captcha: generating one is cheap but should not be a free stream of memory-cache entries.
    options.AddPolicy("captcha", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // forgot-password: capped low per IP — PasswordResetService's own per-account cooldown
    // covers the case of someone rotating IPs to spam a single victim's mailbox; this covers
    // one IP hammering many different accounts.
    options.AddPolicy("forgot-password", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));

    // reset-password: token is 256-bit (brute force is infeasible either way) — this is
    // cheap defense-in-depth, not the primary control.
    options.AddPolicy("reset-password", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------------------------------------------------------------------------
// CORS — UI is a separate app (MVC, React, or anything else), so origins must
// be explicit. Configure AllowedOrigins in appsettings per environment.
// ---------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---------------------------------------------------------------------------
// Problem Details (RFC 7807) for consistent error responses
// ---------------------------------------------------------------------------
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
// OpenAPI (native) + Scalar interactive docs, with JWT bearer support
// ---------------------------------------------------------------------------
// NOTE: Microsoft.AspNetCore.OpenApi's document-transformer surface has shifted a
// little across .NET 9/10 preview releases — if this doesn't compile against the
// exact package version you land on, check the current sample at
// https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnet-openapi
// for "Add a security scheme". The intent below is: publish a "Bearer" scheme and
// require it on every operation so Scalar's "Authorize" button works out of the box.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        var bearerScheme = new OpenApiSecuritySchemeReference("Bearer", document);

        foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations.Values))
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [bearerScheme] = []
            });
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// ---------------------------------------------------------------------------
// License check — ONE TIME at startup, not per request.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var license = scope.ServiceProvider.GetRequiredService<ILicenseService>();
    if (!license.Validate())
    {
        app.Logger.LogCritical("License validation failed. The application will not start.");
        return; // stop startup — do not call app.Run()
    }
}

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // browsable docs at /scalar/v1

    // Dev-only escape hatch for testing the forgot-password flow without a real mailbox —
    // hands back whatever link SimulatedEmailSender last "sent" for a given user. MUST NOT
    // exist outside Development: this is otherwise an unauthenticated account-takeover
    // endpoint (anyone could pull anyone else's live reset link).
    app.MapGet("/api/v1/dev/last-reset-link", (string userName, SimulatedEmailSender sender) =>
        sender.TryGetLastLink(userName, out var link)
            ? Results.Ok(new { userName, link })
            : Results.NotFound())
        .AllowAnonymous();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Serves PosBase > Base Config > Image banner/logo files back to the WASM client — see
// TerminalImageStorageOptions for why this can't just live on local disk like the legacy
// Blazor Server app did. Public/unauthenticated by design (plain static image bytes, no
// sensitive data), same trust level as any other CDN-style asset host.
{
    var imageStorage = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<TerminalImageStorageOptions>>().Value;
    var uploadDir = System.IO.Path.IsPathRooted(imageStorage.UploadDir)
        ? imageStorage.UploadDir
        : System.IO.Path.Combine(AppContext.BaseDirectory, imageStorage.UploadDir);
    Directory.CreateDirectory(uploadDir);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadDir),
        RequestPath = imageStorage.UrlPrefix
    });
}

app.UseCors("Default");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
