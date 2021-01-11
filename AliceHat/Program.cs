using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace AliceHat
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            StartServer();
        }

        private static void StartServer()
        {
            new WebHostBuilder()
                .UseKestrel()
// #if DEBUG
                .ConfigureLogging(build =>
                {
                    build
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddConsole();
                })
// #endif                           
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}