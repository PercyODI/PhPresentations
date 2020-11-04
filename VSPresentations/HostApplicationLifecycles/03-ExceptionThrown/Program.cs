using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileLoggerLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _03_ExceptionThrown
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hbc, lb) =>
                {
                    lb.ClearProviders();
                    lb.AddConsole();
                    lb.AddProvider(new PhLoggingProvider());
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<HostOptions>(o =>
                    {
                        o.ShutdownTimeout = TimeSpan.FromMinutes(5);
                    });
                    services.AddHostedService<Worker>();
                });
    }
}
