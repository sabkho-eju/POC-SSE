using System.Text.Json;
using PocSSE.Backend.WebApi.Infra.Jobs;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Responses;
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
                    var jobStartedData = JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobStarted", DateTime.UtcNow));
                    NotificationQueue.PublishToClient(job.ClientId, new QueuedNotification("JobStarted", jobStartedData));
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                logger.LogInformation("Starting job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);

                // Notification de complétion avec données
                var jobCompletedData = JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobCompleted", DateTime.UtcNow));
                NotificationQueue.PublishToClient(job.ClientId, new QueuedNotification("JobCompleted", jobCompletedData));

                logger.LogInformation("Completed job {JobId} for client {ClientId}", job.JobId, job.ClientId);
            }
        }
    }
}
