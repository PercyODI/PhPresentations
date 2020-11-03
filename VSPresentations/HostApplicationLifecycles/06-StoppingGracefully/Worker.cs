using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _06_StoppingGracefully
{
    public class Worker : BackgroundService
    {
        private IHostApplicationLifetime HostApplicationLifetime { get; }
        private IServiceProvider ServiceProvider { get; }
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider)
        {
            HostApplicationLifetime = hostApplicationLifetime;
            ServiceProvider = serviceProvider;
            _logger = logger;
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
                    tasks.Add(ServiceProvider.GetRequiredService<Consumer>().DoTheWork(consumerId, stoppingToken));
                }

                await Task.WhenAll(tasks);
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

        public async Task DoTheWork(int consumerId, CancellationToken stoppingToken)
        {
            var workId = consumerId * 1000;
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Consumer {consumerId} Starting work on {workId}");
                await Task.Delay(rand.Next(5_000, 10_000));

                _logger.LogInformation($"Consumer {consumerId} Finished work on {workId++}");
            }
        }
    }
}