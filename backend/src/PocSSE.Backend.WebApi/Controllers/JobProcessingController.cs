using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Jobs;
using PocSSE.Backend.WebApi.Models.API.Requests;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Models.Entities;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;

namespace PocSSE.Backend.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobProcessingController(
    BackgroundJobQueue backgroundJobQueue,
    ILogger<JobProcessingController> logger) : ControllerBase
{
    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessJob([FromBody] JobRequest request)
    {
        var username = GetAuthenticatedUsername();

        logger.LogInformation("Processing job: {JobId} for user: {Username}", request.JobId, username);

        await backgroundJobQueue.QueueAsync(new QueuedJob(
            JobId: request.JobId,
            ClientId: username,
            Description: request.JobData,
            DurationSeconds: request.DurationSeconds));

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
            var (sId, channelReader) = NotificationQueue.Subscribe(userName);
            subscriptionId = sId;

            logger.LogInformation("Client {Username} connected with subscription {SubscriptionId}", userName, sId);

            var notificationStream = Notifications(userName, channelReader, cancellationToken);

            NotificationQueue.PublishToClient(userName, new QueuedNotification("Connected", null));

            return Results.ServerSentEvents(notificationStream, eventType: "JobNotification");
        }
        catch (OperationCanceledException operationCanceledException)
        {
            logger.LogInformation(operationCanceledException, "Notification stream for user {Username} was cancelled", userName);
            NotificationQueue.PublishToClient(userName, new QueuedNotification("Disconnected", null));
            Unsubscribe(subscriptionId, userName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in notification stream for user {Username}", userName);
            NotificationQueue.PublishToClient(userName, new QueuedNotification("Disconnected", null));
            Unsubscribe(subscriptionId, userName);
        }

        return Results.NoContent();
    }

    private void Unsubscribe(Guid subscriptionId, string userName)
    {
        if (subscriptionId != Guid.Empty && !string.IsNullOrEmpty(userName))
        {
            NotificationQueue.Unsubscribe(userName, subscriptionId);
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

            // Optionnel : extraire le timestamp des données si disponible
            if (data.TryGetProperty("completedAt", out var completedAtElement))
            {
                if (DateTime.TryParse(completedAtElement.GetString(), out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
            }
        }

        return new JobResponse(
            JobId: jobId,
            Status: queuedNotification.EventName,
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

