using SyncNet.Template;
using SyncNetPro.Sdk;
#if (WithRouting)
using SyncNetPro.Routing;
#endif

// Host SyncNet: konfigurasi node dari database Core (Production) atau appsettings.Development.json + SimCore (Development).
var builder = SyncNetApplication.CreateBuilder(args);
builder.AddSyncNetInterface<ChannelInterface>();
builder.Services.AddChannelServices();
#if (WithRouting)

// Routing biller & fee (tabel sw_routes_*, Product > Fees di database Core); dimuat saat start dan RESYNC.
builder.AddSyncNetRouting();
builder.Services.AddSingleton<SyncNet.Template.Routing.RoutingStep>();
#endif

await builder.Build().RunAsync();
