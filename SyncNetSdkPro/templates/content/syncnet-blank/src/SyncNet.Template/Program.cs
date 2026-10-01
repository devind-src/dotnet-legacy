using SyncNet.Template;
using SyncNetPro.Sdk;

// Host SyncNet: konfigurasi node dari database Core (Production) atau appsettings.Development.json + SimCore (Development).
var builder = SyncNetApplication.CreateBuilder(args);
builder.AddSyncNetInterface<MyInterface>();

// Daftarkan layanan sendiri di sini (mapping, klien, options), mis. builder.Services.AddSingleton<MyMapper>();

await builder.Build().RunAsync();
