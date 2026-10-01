using SyncNet.Template;
using SyncNetPro.Sdk;

// Host SyncNet: konfigurasi node dari database Core (Production) atau appsettings.Development.json + SimCore (Development).
var builder = SyncNetApplication.CreateBuilder(args);
builder.AddSyncNetInterface<AcquirerInterface>();
builder.Services.AddAcquirerServices();

await builder.Build().RunAsync();
