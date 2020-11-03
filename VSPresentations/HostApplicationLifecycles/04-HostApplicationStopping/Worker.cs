using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _04_HostApplicationStopping
{
    public class Worker : BackgroundService
    {
        private IHostApplicationLifetime HostApplicationLifetime { get; }
        private readonly ILogger<Worker> _logger;
        private readonly Random rand;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            HostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            rand = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting ExecuteAsync()");
                var workId = 0;
                while (!stoppingToken.IsCancellationRequested)
                {

                    _logger.LogInformation($"Starting work on {workId}");
                    await Task.Delay(rand.Next(500, 2_500));
                    if (workId == 5)
                    {
                        _logger.LogError("Throwing an exception...");
                        throw new Exception("Uh Oh!");
                    }

                    _logger.LogInformation($"Finished work on {workId++}");
                }
            }
            catch (Exception e)
            {
                HostApplicationLifetime.StopApplication();
            }
        }
    }
}
