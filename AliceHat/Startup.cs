using System;
using AliceHat.Models;
using AliceHat.Services;
using AliceHat.Services.Abstract;
using Microsoft.AspNetCore.Builder;
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
            
            services.AddSingleton<IDbService, MongoService>();
            services.AddSingleton<ContentService>();
            services.AddSingleton<AliceService>();
            services.AddSingleton<TelegramService>();
            services.AddSingleton<GameplayService>();
        }

        public void Configure(IApplicationBuilder app, IDbService dbService, ContentService contentService, TelegramService telegramService)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            
            dbService.Init("alicehat", type =>
            {
                if (type == typeof(WordData)) return "words";
                if (type == typeof(TgUser)) return "tgusers";
                throw new ArgumentOutOfRangeException(nameof(type), $"No collection for type: {type.FullName}");
            });

            contentService.LoadWords();
            
            telegramService.Start();
        }
    }
}