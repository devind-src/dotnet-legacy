using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using SyncNet.Common;
using SyncNet.DbRepository;
using SyncNet.Logging;
using SyncNet.Networking;
using SyncNet.Services;
using System.Threading.Channels;

namespace SyncNet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HSM Service", Version = "v1.0" });
            });

            //encoding for pdfsharp
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //custom service
            services.AddControllers();

            //register services: same object any request
            services.AddSingleton<CustomLogger>();
            services.AddSingleton<DbService>();
            services.AddSingleton<DbMgr>();

            services.AddSingleton<XTcpClient>();
            services.AddSingleton<ApiClient>();

            services.AddSingleton<HsmThales>();
            services.AddSingleton<HsmService>();
            services.AddSingleton<HsmEmulator>();

            services.AddSingleton(Channel.CreateUnbounded<string>());

            // background service
            services.AddHostedService<HsmHostedService>();
            services.AddHostedService<LogWriterService>();

            //register services: new object per request
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "HSM Service");
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            //log request & response
            if (AppConfig.TraceOn == true)
                app.UseMiddleware<LogTrace>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
