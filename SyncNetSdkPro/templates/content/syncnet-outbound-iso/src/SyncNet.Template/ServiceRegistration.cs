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

    /// <summary>Kode institusi acquirer yang dikirim di field 32.</summary>
    public string AcquirerId { get; set; } = "008";

    /// <summary>Kirim sign-on (0800/001) saat koneksi terbentuk.</summary>
    public bool SignOn { get; set; } = true;
}
