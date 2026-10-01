using Microsoft.Extensions.DependencyInjection;
using SyncNet.Template.Mapping;

namespace SyncNet.Template;

/// <summary>Registrasi layanan interface (dipakai Program.cs dan test).</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddChannelServices(this IServiceCollection services)
    {
        services.AddOptions<ChannelOptions>().BindConfiguration(ChannelOptions.SectionName);
        services.AddSingleton<ToCore>();
        services.AddSingleton<ToChannel>();
        return services;
    }
}

/// <summary>Konfigurasi khusus channel (<c>appsettings.json</c> bagian <c>Channel</c>).</summary>
public sealed class ChannelOptions
{
    public const string SectionName = "Channel";

    /// <summary>Nama header API key. Nilai yang diharapkan = <c>sw_connections.ws_key</c> (kosong = tanpa otentikasi).</summary>
    public string ApiKeyHeader { get; set; } = "X-Api-Key";

    /// <summary><c>acq_inst_id</c> yang dikirim ke Core.</summary>
    public string AcquirerId { get; set; } = "008";
}
