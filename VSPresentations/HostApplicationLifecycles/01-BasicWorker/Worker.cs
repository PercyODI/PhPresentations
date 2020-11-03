using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FileLoggerLibrary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _01_BasicWorker
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
                _logger.LogInformation($"Starting work on {workId}");
                Thread.Sleep(rand.Next(500, 2_500));
                _logger.LogInformation($"Finished work on {workId++}");
            }
        }
    }
}
