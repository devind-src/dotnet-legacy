using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncNet.DbRepository;
using SyncNet.Services;
using System;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace SyncNet
{ 
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal Error: {ex.Message}");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Otomatis aktif pada Windows Service
                .UseSystemd()        // Otomatis aktif pada Linux Systemd Daemon
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MyApp>();
                    services.AddSingleton(Channel.CreateUnbounded<string>());

                    services.AddHostedService<AppService>();
                    services.AddHostedService<TcpLogQueueService>();
                });
    }
}
