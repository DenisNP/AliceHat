using AliceHat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AliceHat
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });
            
            services.AddSingleton<ContentService>();
            services.AddSingleton<AliceService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ContentService contentService)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            contentService.LoadWords(@"words.csv");
        }
    }
}