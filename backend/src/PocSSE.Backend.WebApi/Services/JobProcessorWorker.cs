using PocSSE.Backend.WebApi.Infra;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Services
{
    public sealed class JobProcessorWorker(
        BackgroundJobQueue queue,
        ILogger<JobProcessorWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                QueuedJob job;
                try
                {
                    job = await queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                logger.LogInformation("Starting job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);
                
                logger.LogInformation("Completed job {JobId} for client {ClientId}", job.JobId, job.ClientId);
            }
        }
    }
}
