using PocSSE.Backend.WebApi.Infra;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Services
{
    public sealed class JobProcessorWorker(
        BackgroundJobQueue queue,
        JobNotificationService jobNotificationService,
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

                    logger.LogInformation("Starting job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                    await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);

                    jobNotificationService.PublishToClient(
                        job.ClientId,
                        new JobNotification(
                            EventName: "job-completed",
                            Message: $"Traitement '{job.Description}' termine.",
                            Status: "Completed",
                            JobId: job.JobId,
                            CompletedAt: DateTimeOffset.UtcNow));

                    logger.LogInformation("Completed job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
