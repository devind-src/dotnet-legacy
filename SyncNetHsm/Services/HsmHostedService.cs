using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    public class HsmHostedService : IHostedService
    {
        private readonly HsmService _hsmService;
        public HsmHostedService(HsmService hsmService)
        {
            _hsmService = hsmService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _hsmService.Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
