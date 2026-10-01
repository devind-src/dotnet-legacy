using Microsoft.Extensions.DependencyInjection;
using SyncNet.Template.Mapping;

namespace SyncNet.Template;

/// <summary>Registrasi layanan interface (dipakai Program.cs dan test).</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddBillerServices(this IServiceCollection services)
    {
        services.AddOptions<BillerOptions>().BindConfiguration(BillerOptions.SectionName);
        services.AddSingleton<ToRemote>();
        services.AddSingleton<ToCore>();
        return services;
    }
}

/// <summary>Konfigurasi khusus biller (<c>appsettings.json</c> bagian <c>Biller</c>).</summary>
public sealed class BillerOptions
{
    public const string SectionName = "Biller";

    /// <summary>Kode mitra yang dikirim ke biller.</summary>
    public string PartnerId { get; set; } = "SYNCNET";

    /// <summary>Header tambahan (mis. API key). Kosong = memakai <c>sw_connections.ws_key</c> sebagai header X-Api-Key.</summary>
    public Dictionary<string, string> Headers { get; set; } = [];
}
