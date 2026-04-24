using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Requests;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Models.Entities;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using PocSSE.Backend.WebApi.Infra.Notifications;

namespace PocSSE.Backend.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobProcessingController(
    BackgroundJobQueue backgroundJobQueue,
    NotificationQueue NotificationQueue,
    NotificationQueue notificationQueue,
    ILogger<JobProcessingController> logger) : ControllerBase
{
    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessJob([FromBody] JobRequest request)
    {
        var username = GetAuthenticatedUsername();

        logger.LogInformation("Processing job: {JobId} for user: {Username}", request.JobId, username);

        notificationQueue.Publish(string.Empty, new QueuedNotification("JobProcessing",
            Data: JsonSerializer.SerializeToElement(new JobProcessingDescriptor(
                JobId: request.JobId,
                ClientId: username,
                Description: request.JobData,
                DurationSeconds: request.DurationSeconds))));

        return Ok(new JobResponse(request.JobId, "JobQueued", DateTime.UtcNow));
    }

    [HttpPost("cancel")]
    [Authorize]
    public IActionResult Cancel(string jobId)
    {
        var username = GetAuthenticatedUsername();
        logger.LogInformation("Cancelling job: {JobId} for user: {Username}", jobId, username);

        //ToDo : implement action

        return Ok($"Cancelled {jobId}");
    }

    [HttpGet("job-notification-stream")]
    [Authorize]
    public IResult GetNotificationStream(CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.Empty;
        var userName = string.Empty;
        try
        {
            userName = GetAuthenticatedUsername();
            var (sId, channelReader) = NotificationQueue.Subscribe("JobNotification", userName);
            subscriptionId = sId;

            logger.LogInformation("Client {Username} connected with subscription {SubscriptionId}", userName, sId);

            var notificationStream = Notifications(userName, channelReader, cancellationToken);

            var connectionMessageData = JsonSerializer.SerializeToElement(new JobResponse(string.Empty, "Connected", DateTime.UtcNow));
            NotificationQueue.Publish(userName, new QueuedNotification("JobNotification", connectionMessageData));

            return Results.ServerSentEvents(notificationStream, eventType: "JobNotification");
        }
        catch (OperationCanceledException operationCanceledException)
        {
            logger.LogInformation(operationCanceledException, "Notification stream for user {Username} was cancelled", userName);
            var connectionMessageData = JsonSerializer.SerializeToElement(new JobResponse(string.Empty, "Disconnected", DateTime.UtcNow));
            NotificationQueue.Publish(userName, new QueuedNotification("JobNotification", connectionMessageData));
            Unsubscribe(subscriptionId, userName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in notification stream for user {Username}", userName);
            var connectionMessageData = JsonSerializer.SerializeToElement(new JobResponse(string.Empty, "Disconnected", DateTime.UtcNow));
            NotificationQueue.Publish(userName, new QueuedNotification("JobNotification", connectionMessageData));
            Unsubscribe(subscriptionId, userName);
        }

        return Results.NoContent();
    }

    private void Unsubscribe(Guid subscriptionId, string userName)
    {
        if (subscriptionId != Guid.Empty && !string.IsNullOrEmpty(userName))
        {
            NotificationQueue.Unsubscribe(subscriptionId);
            logger.LogInformation("Client {Username} disconnected, unsubscribed {SubscriptionId}", userName, subscriptionId);
        }
    }

    private async IAsyncEnumerable<JobResponse> Notifications(string userName, ChannelReader<QueuedNotification> channelReader, CancellationToken cancellationToken)
    {
        await foreach (var notification in channelReader.ReadAllAsync(cancellationToken))
        {
            logger.LogInformation("Sending job notification to client {Username}: {Notification}", userName, JsonSerializer.SerializeToElement(notification).ToString());
            yield return MapToJobResponse(notification);
        }
    }

    private JobResponse MapToJobResponse(QueuedNotification queuedNotification)
    {
        var jobId = "unknown";
        var status = "unknown";
        var timestamp = DateTime.UtcNow;

        if (queuedNotification.Data.HasValue)
        {
            var data = queuedNotification.Data.Value;

            // Extraire JobId (case-insensitive)
            if (data.TryGetProperty("jobId", out var jobIdElement))
            {
                jobId = jobIdElement.GetString() ?? "unknown";
            }
            else if (data.TryGetProperty("JobId", out var jobIdElementCaps))
            {
                jobId = jobIdElementCaps.GetString() ?? "unknown";
            }

            if (data.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetString() ?? "unknown";
            }
            else if (data.TryGetProperty("Status", out var statusElementCaps))
            {
                status = statusElementCaps.GetString() ?? "unknown";
            }

            // Optionnel : extraire le timestamp des données si disponible
            if (data.TryGetProperty("timestamp", out var completedAtElement))
            {
                if (DateTime.TryParse(completedAtElement.GetString(), out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
            }
            else if (data.TryGetProperty("Timestamp", out var completedAtElementCaps))
            {
                if (DateTime.TryParse(completedAtElementCaps.GetString(), out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
            }

        }

        return new JobResponse(
            JobId: jobId,
            Status: status,
            Timestamp: timestamp
        );
    }

    private string GetAuthenticatedUsername()
    {
        return User.Identity?.Name
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? throw new UnauthorizedAccessException("User identity not found");
    }
}

