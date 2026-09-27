using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SyncNet.Common;
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
            #if RELEASE
                    //if (System.Diagnostics.Debugger.IsAttached)
                    //{
                    //    //debugger terdeteksi
                    //    Environment.Exit(1);
                    //}
            #endif

            // build config
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // load config
            AppConfig.Initialize(config);
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              .ConfigureWebHost(webBuilder =>
              {
                  webBuilder
                     .UseKestrel() 
                     .UseStartup<Startup>();

                  if (!string.IsNullOrEmpty(AppConfig.UrlListen))
                      webBuilder.UseUrls(AppConfig.UrlListen);
              })
              .UseWindowsService()   // aktif kalau di Windows
              .UseSystemd();         // aktif kalau di Linux
    }
}
