using Microsoft.Extensions.DependencyInjection;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk;

/// <summary>
/// Layanan bersama untuk konteks handler. Di-resolve malas dari DI agar kanal Core dan koneksi remote
/// dapat saling memakai tanpa dependensi melingkar.
/// </summary>
public sealed class SyncNetServices(IServiceProvider provider)
{
    private readonly Lazy<ICoreClient> _core = new(provider.GetRequiredService<ICoreClient>);
    private readonly Lazy<IRemoteRegistry> _remote = new(provider.GetRequiredService<IRemoteRegistry>);
    private readonly Lazy<ITraceWriter> _trace = new(provider.GetRequiredService<ITraceWriter>);

    /// <summary>Klien Core.</summary>
    public ICoreClient Core => _core.Value;

    /// <summary>Koneksi remote.</summary>
    public IRemoteRegistry Remote => _remote.Value;

    /// <summary>Trace.</summary>
    public ITraceWriter Trace => _trace.Value;

    /// <summary>Service provider.</summary>
    public IServiceProvider Provider { get; } = provider;
}
