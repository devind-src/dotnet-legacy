using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncNet.Common;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    internal class LogService
    {
        private IHost _host;
        private Channel<string> _channel;

        public bool IsRunning()
        {
            return (_host == null) ? false : true;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_host != null)
                throw new InvalidOperationException("Log service is running");

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    _channel = Channel.CreateUnbounded<string>();
                    services.AddSingleton(_channel);

                    if (SdkConfig.Settings.RabbitMQ.Enable == true)
                        services.AddHostedService<RabbitLogWorker>();
                    else
                        services.AddHostedService<TcpLogWorker>();
                })
                .Build();

            await _host.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_host != null)
            {
                await _host.StopAsync(cancellationToken);

                _host.Dispose();
                _host = null;
                _channel = null;
            }
        }

        public async Task SendAsync(string msg, CancellationToken cancellationToken = default)
        {
            if (_channel == null)
                throw new InvalidOperationException("Log service has not been started");

            await _channel.Writer.WriteAsync(msg, cancellationToken);
        }
    }

}
