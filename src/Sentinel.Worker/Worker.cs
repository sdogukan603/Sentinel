
using Sentinel.Core.Services;

namespace Sentinel.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("SENTINEL Worker started.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = scopeFactory.CreateScope();

                var jobService = scope.ServiceProvider
                    .GetRequiredService<JobService>();

                logger.LogInformation(
                    "Worker cycle completed at {time}",
                    DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }

        logger.LogInformation("SENTINEL Worker stopped.");
    }
}
