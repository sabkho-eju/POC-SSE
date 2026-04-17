using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Models.API.Requests;
using PocSSE.Backend.WebApi.Models.Entities;
using PocSSE.Backend.WebApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PocSSE.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceEventNotificationController(
        JobNotificationService jobNotificationService,
        ILogger<ServiceEventNotificationController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Server-Sent Events stream endpoint
        /// </summary>
        /// <param name="clientId">Unique client identifier</param>
        /// <param name="cancellationToken">Cancellation token for connection lifetime</param>
        [HttpGet("ssestream")]
        [Authorize]
        public async Task SseStream(CancellationToken cancellationToken)
        {
            var clientId = GetAuthenticatedUsername() ?? "anonymous";
            logger.LogInformation("SSE connection started: ClientId={ClientId}", clientId);

            // Validation 
            if (string.IsNullOrWhiteSpace(clientId))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("clientId query parameter is required", cancellationToken);
                return;
            }

            // Configuration SSE headers
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.Append("X-Accel-Buffering", "no"); // Nginx/Apache buffering

            var (subscriptionId, reader) = jobNotificationService.Subscribe(clientId);
            logger.LogInformation("SSE subscription created: SubscriptionId={SubscriptionId}, ClientId={ClientId}", subscriptionId, clientId);

            try
            {
                // Initial event to confirm connection
                await WriteEventAsync(clientId, "connected", new
                {
                    clientId,
                    subscriptionId = subscriptionId.ToString(),
                    message = "SSE connection established",
                    completedAt = DateTime.UtcNow
                }, cancellationToken);

                // Main loop for reading notifications and sending keep-alives
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Attendre un événement ou timeout pour keep-alive
                    var waitTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                    var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                    if (completedTask == waitTask)
                    {
                        if (await waitTask)
                        {
                            // Read all available notifications
                            while (reader.TryRead(out var notification))
                            {
                                await WriteEventAsync(clientId, notification.EventName, notification, cancellationToken);
                            }
                        }
                        else
                        {
                            // closed channel
                            logger.LogInformation("SSE channel closed for ClientId={ClientId}", clientId);
                            break;
                        }
                    }
                    else
                    {
                        // Timeout - send keep-alive comment
                        await Response.WriteAsync(": keep-alive\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                        logger.LogTrace("SSE keep-alive sent to ClientId={ClientId}", clientId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("SSE connection cancelled: ClientId={ClientId}", clientId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SSE error for ClientId={ClientId}", clientId);

                // Try to send an error event to the client before closing the connection
                try
                {
                    await WriteEventAsync(clientId, "error", new
                    {
                        message = "Connection error occurred",
                        timestamp = DateTime.UtcNow
                    }, cancellationToken);
                }
                catch
                {
                    // Ignore any exceptions while trying to send the error event, as the connection may already be broken
                }
            }
            finally
            {
                jobNotificationService.Unsubscribe(clientId, subscriptionId);
                logger.LogInformation("SSE connection closed: ClientId={ClientId}, SubscriptionId={SubscriptionId}", clientId, subscriptionId);
            }
        }

        /// <summary>
        /// Send a notification to a specific client
        /// </summary>
        /// <param name="clientId">Target client identifier</param>
        /// <param name="request">Notification details</param>
        [HttpPost("send")]
        [Authorize]
        public IActionResult SendToClient([FromBody] SendNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                return BadRequest(new { Error = "clientId is required" });
            }

            if (request == null)
            {
                return BadRequest(new { Error = "Request body is required" });
            }

            var notification = new JobNotification(
                EventName: request.EventName,
                JobId: request.JobId,
                Message: request.Message,
                Status: request.Status,
                CompletedAt: DateTime.UtcNow
            );

            jobNotificationService.PublishToClient(request.ClientId, notification);

            logger.LogInformation(
                "Notification sent to client {ClientId}: Event={EventName}, JobId={JobId}",
                request.ClientId, request.EventName, request.JobId);

            return Ok(new
            {
                Success = true,
                Message = $"Notification sent to client '{request.ClientId}'",
                ClientId = request.ClientId,
                EventName = request.EventName,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Broadcast a notification to all connected clients
        /// </summary>
        /// <param name="request">Notification details</param>
        [HttpPost("broadcast")]
        [Authorize]
        public IActionResult Broadcast([FromBody] SendNotificationRequest request)
        {
            var notification = new JobNotification(
                EventName: request.EventName,
                JobId: request.JobId,
                Message: request.Message,
                Status: request.Status,
                CompletedAt: DateTime.UtcNow
            );

            jobNotificationService.Broadcast(notification);

            logger.LogInformation(
                "Notification broadcast to all clients: Event={EventName}, JobId={JobId}",
                request.EventName, request.JobId);

            return Ok(new
            {
                Success = true,
                Message = "Notification broadcast to all connected clients",
                EventName = request.EventName,
                Timestamp = DateTime.UtcNow
            });
        }

        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }

        // Helper for writing SSE events in the correct format
        private async Task WriteEventAsync(string clientId, string eventName, object payload, CancellationToken cancellationToken)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                logger.LogDebug("SSE event sent: {EventName} to ClientId={ClientId}", eventName, clientId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to write SSE event {EventName} to ClientId={ClientId}", eventName, clientId);
                throw;
            }
        }
    }
}