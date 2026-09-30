using Sample.Outbound;
using SyncNetPro.Sdk;

var builder = SyncNetApplication.CreateBuilder(args);

builder.AddSyncNetInterface<EchoBillerInterface>();

await builder.Build().RunAsync();
