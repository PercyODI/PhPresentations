using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _05_WatchForThreads
{
    public class Worker : BackgroundService
    {
        private IHostApplicationLifetime HostApplicationLifetime { get; }
        private IServiceProvider ServiceProvider { get; }
        private readonly ILogger<Worker> _logger;
        private readonly Random rand;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider)
        {
            HostApplicationLifetime = hostApplicationLifetime;
            ServiceProvider = serviceProvider;
            _logger = logger;
            rand = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>();
            try
            {
                _logger.LogInformation("Starting ExecuteAsync()");
                var consumerIds = new[] {1, 2, 3, 4};
                foreach (var consumerId in consumerIds)
                {
                    tasks.Add(ServiceProvider.GetRequiredService<Consumer>().DoTheWork(consumerId));
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                HostApplicationLifetime.StopApplication();
            }
        }
    }

    public class Consumer
    {
        private readonly ILogger<Worker> _logger;
        private readonly Random rand;

        public Consumer(ILogger<Worker> logger)
        {
            _logger = logger;
            rand = new Random();
        }

        public async Task DoTheWork(int consumerId)
        {
            var workId = consumerId * 1000;
            while (true)
            {
                _logger.LogInformation($"Consumer {consumerId} Starting work on {workId}");
                await Task.Delay(rand.Next(3_000, 5_500));
                if (workId == consumerId * 1000 + 5)
                {
                    _logger.LogError($"Consumer {consumerId} Throwing an exception...");
                    throw new Exception("Uh Oh!");
                }

                _logger.LogInformation($"Consumer {consumerId} Finished work on {workId++}");
            }
        }
    }
}