using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Services.Monitoring;

namespace SyncNetApi.Services.Routing
{
    public interface IRoutingApplyService
    {
        Task<RoutingApplyResultDto> ApplyAsync();
    }

    /// <summary>Tombol "Terapkan Perubahan" (keputusan K14/K22): kirim RESYNC ke setiap aplikasi
    /// yang memakai routing supaya perubahan fee/biller/mode dari dashboard langsung dipakai.
    /// Daftar aplikasi dari konfigurasi Routing:ResyncApps (default "API Channel"). Kegagalan satu
    /// aplikasi tidak menghentikan yang lain.</summary>
    public class RoutingApplyService : IRoutingApplyService
    {
        private static readonly string[] DefaultApps = ["API Channel"];

        private readonly IMonitoringCommandService _commands;
        private readonly IConfiguration _configuration;

        public RoutingApplyService(IMonitoringCommandService commands, IConfiguration configuration)
        {
            _commands = commands;
            _configuration = configuration;
        }

        public async Task<RoutingApplyResultDto> ApplyAsync()
        {
            var apps = _configuration.GetSection("Routing:ResyncApps").Get<string[]>();
            if (apps == null || apps.Length == 0) apps = DefaultApps;

            var results = new List<RoutingApplyItemDto>();
            foreach (var app in apps.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct())
            {
                try
                {
                    var response = await _commands.SendApplicationCommandAsync(app, "RESYNC");
                    results.Add(new RoutingApplyItemDto(app, true, string.IsNullOrWhiteSpace(response) ? "OK" : response.Trim()));
                }
                catch (Exception ex)
                {
                    results.Add(new RoutingApplyItemDto(app, false, ex.Message));
                }
            }

            return new RoutingApplyResultDto(results);
        }
    }
}
