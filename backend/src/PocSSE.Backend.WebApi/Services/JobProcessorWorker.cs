using PocSSE.Backend.WebApi.Infra.Jobs;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Services
{
    public sealed class JobProcessorWorker(
        BackgroundJobQueue queue,
        NotificationQueue NotificationQueue,
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
                    NotificationQueue.PublishToClient(job.ClientId, new QueuedNotification("JobStarted", $"Job {job.JobId} started", null));
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                logger.LogInformation("Starting job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);
                NotificationQueue.PublishToClient(job.ClientId, new QueuedNotification("JobCompleted", $"Job {job.JobId} completed", null));
                logger.LogInformation("Completed job {JobId} for client {ClientId}", job.JobId, job.ClientId);
            }
        }
    }
}
