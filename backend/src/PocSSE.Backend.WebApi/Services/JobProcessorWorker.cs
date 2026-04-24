using System.Text.Json;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Services
{
    public sealed class JobProcessorWorker(
        NotificationQueue notificationQueue,
        ILogger<JobProcessorWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var (sId, channelReader) = notificationQueue.Subscribe("JobProcessing", null);
            while (!cancellationToken.IsCancellationRequested)
            {
                var notification = await channelReader.ReadAsync(cancellationToken);

                var job = MapToJobProcessingDescriptor(notification);

                var jobStartedData =
                    JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobStarted", DateTime.UtcNow));
                notificationQueue.Publish(job.ClientId, new QueuedNotification("JobNotification", jobStartedData));

                await ProcessJobAsync(job, cancellationToken);

                var jobCompletedData =
                    JsonSerializer.SerializeToElement(new JobResponse(job.JobId, "JobCompleted", DateTime.UtcNow));
                notificationQueue.Publish(job.ClientId, new QueuedNotification("JobNotification", jobCompletedData));
            }
            notificationQueue.Unsubscribe(sId);
        }

        private JobProcessingDescriptor MapToJobProcessingDescriptor(QueuedNotification notification)
        {
            var clientId = string.Empty;
            var jobId = string.Empty;
            var description = string.Empty;
            var durationSeconds = 0;

            if (notification.Data.HasValue)
            {
                var data = notification.Data.Value;

                if (data.TryGetProperty("clientId", out var clientIdElement))
                {
                    clientId = clientIdElement.GetString() ?? "unknown";
                }
                else if (data.TryGetProperty("ClientId", out var clientIdElementCaps))
                {
                    clientId = clientIdElementCaps.GetString() ?? "unknown";
                }

                if (data.TryGetProperty("jobId", out var jobIdElement))
                {
                    jobId = jobIdElement.GetString() ?? "unknown";
                }
                else if (data.TryGetProperty("JobId", out var jobIdElementCaps))
                {
                    jobId = jobIdElementCaps.GetString() ?? "unknown";
                }

                if (data.TryGetProperty("description", out var statusElement))
                {
                    description = statusElement.GetString() ?? "unknown";
                }
                else if (data.TryGetProperty("Description", out var statusElementCaps))
                {
                    description = statusElementCaps.GetString() ?? "unknown";
                }

                if (data.TryGetProperty("durationSeconds", out var durationSecondsAtElement))
                {
                    durationSeconds = durationSecondsAtElement.GetInt32();

                }
                else if (data.TryGetProperty("DurationSeconds", out var durationSecondsElementCaps))
                {
                    durationSeconds = durationSecondsElementCaps.GetInt32();
                }
            }

            return new JobProcessingDescriptor(
                ClientId: clientId,
                JobId: jobId,
                Description: description,
                DurationSeconds: durationSeconds
            );
        }

        private async Task ProcessJobAsync(JobProcessingDescriptor jobProcessingDescriptor, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Starting job Processing {JobId} for client {ClientId}", jobProcessingDescriptor.JobId, jobProcessingDescriptor.ClientId);

                await Task.Delay(TimeSpan.FromSeconds(jobProcessingDescriptor.DurationSeconds), cancellationToken);

                logger.LogInformation("Completed job Processing {JobId} for client {ClientId}", jobProcessingDescriptor.JobId, jobProcessingDescriptor.ClientId);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Stopping jobProcessingDescriptor processing due to cancellation.");
            }
            catch (Exception e)
            {
                var jobId = jobProcessingDescriptor?.JobId ?? "Unknown";
                var clientId = jobProcessingDescriptor?.ClientId ?? "Unknown";
                logger.LogError(e, "An error occurred while processing jobProcessingDescriptor {JobId}.", jobId);
                var jobFailedData =
                    JsonSerializer.SerializeToElement(new JobResponse(jobId, "JobFailed", DateTime.UtcNow));
                notificationQueue.Publish(clientId, new QueuedNotification("JobNotification", jobFailedData));
            }

        }
    }
}
