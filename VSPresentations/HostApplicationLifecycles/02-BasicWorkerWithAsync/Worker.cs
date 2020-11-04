using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileLoggerLibrary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _02_BasicWorkerWithAsync
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Random rand;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            rand = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting ExecuteAsync()");
            var workId = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Starting work on {workId}");
                    await Task.Delay(rand.Next(5_000, 6_500), stoppingToken);
                    _logger.LogInformation($"Finished work on {workId++}");
                }
                catch (Exception e)
                {
                    _logger.LogError($"There was an error! {e}");
                }
            }
        }
    }
}