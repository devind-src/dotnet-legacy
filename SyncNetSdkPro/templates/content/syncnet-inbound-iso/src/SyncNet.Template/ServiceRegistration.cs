using Microsoft.Extensions.DependencyInjection;
using SyncNet.Template.Mapping;

namespace SyncNet.Template;

/// <summary>Registrasi layanan interface (dipakai Program.cs dan test).</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddAcquirerServices(this IServiceCollection services)
    {
        services.AddSingleton<ToCore>();
        services.AddSingleton<ToIso>();
        return services;
    }
}
