using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet
{
    internal class AppService : BackgroundService
    {
        private readonly ILogger<AppService> _logger;
        private readonly MyApp _app;

        public AppService(ILogger<AppService> logger, MyApp app)
        {
            _app = app;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Start service {MyApp.APPNAME} {MyApp.VERSION}");

            try
            {
                // Kirim stoppingToken bawaan OS ke aplikasi
                await _app.Start(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service shutdown by OS.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error ini service");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stop service {MyApp.APPNAME} {MyApp.VERSION}");

            try
            {
                await _app.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
