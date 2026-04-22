using PocSSE.Backend.WebApi.Infra.Jobs;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Models.Entities;
using System.Text.Json;

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
                QueuedJob? job = null;
                try
                {
                    job = await queue.DequeueAsync(stoppingToken);
                    var jobData =
                        JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobStarted", DateTime.UtcNow));
                    NotificationQueue.PublishToClient(job.ClientId, new QueuedNotification("JobStarted", jobData));

                    logger.LogInformation("Starting job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                    await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);
                    var jobCompletedData =
                        JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobCompleted", DateTime.UtcNow));
                    NotificationQueue.PublishToClient(job.ClientId,
                        new QueuedNotification("JobCompleted", jobCompletedData));
                    logger.LogInformation("Completed job {JobId} for client {ClientId}", job.JobId, job.ClientId);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Job processor worker is stopping due to cancellation.");
                    break;
                }
                catch (Exception e)
                {
                    var jobId = job?.JobId ?? "Unknown";
                    var clientId = job?.ClientId ?? "Unknown";
                    logger.LogError(e, "An error occurred while processing job {JobId}.", jobId);
                    var jobFailedData =
                        JsonSerializer.SerializeToElement(new JobResponse(jobId, "JobFailed", DateTime.UtcNow));
                    NotificationQueue.PublishToClient(clientId, new QueuedNotification("JobFailed", jobFailedData));
                }
            }
        }
    }
}
