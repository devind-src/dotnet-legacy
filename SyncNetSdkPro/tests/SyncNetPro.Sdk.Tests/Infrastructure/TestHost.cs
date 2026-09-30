using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Hosting;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Tests.Infrastructure;

/// <summary>Menjalankan interface nyata (host lengkap) terhadap <see cref="FakeCore"/> dalam mode JSON.</summary>
internal sealed class TestHost<THandler> : IAsyncDisposable
    where THandler : SyncNetInterface
{
    private readonly IHost _host;
    private readonly TempDirectory _home;
    private bool _stopped;

    private TestHost(IHost host, TempDirectory home)
    {
        _host = host;
        _home = home;
    }

    public IServiceProvider Services => _host.Services;

    public THandler Handler => Services.GetRequiredService<THandler>();

    public SyncNetRuntime Runtime => Services.GetRequiredService<SyncNetRuntime>();

    public string Home => _home.Path;

    public static async Task<TestHost<THandler>> StartAsync(
        IEnumerable<NodeInfo> nodes,
        Action<SyncNetOptions>? configure = null,
        Action<IServiceCollection>? services = null)
    {
        var home = new TempDirectory();
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        services?.Invoke(builder.Services);
        builder.AddSyncNetInterface<THandler>(o =>
        {
            o.AppName = "Test App";
            o.Version = "v9.9-test";
            o.Home = home.Path;
            o.NodeSource = NodeSourceKind.Json;
            o.Nodes = [.. nodes];
            o.Command.BindAddress = "127.0.0.1";
            o.Command.Port = 0;
            o.Core.ReconnectDelay = TimeSpan.FromMilliseconds(100);
            o.Core.ResponseTimeoutMargin = TimeSpan.Zero;
            configure?.Invoke(o);
        });

        IHost host = builder.Build();
        await host.StartAsync();
        return new TestHost<THandler>(host, home);
    }

    public async Task StopAsync()
    {
        if (_stopped) return;
        _stopped = true;
        await _host.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _host.Dispose();
        _home.Dispose();
    }
}
