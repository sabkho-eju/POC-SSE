using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Notifications;
using System.Security.Claims;
using System.Text.Json;

namespace PocSSE.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceNotificationController(
        NotificationQueue NotificationQueue,
        ILogger<ServiceNotificationController> logger) : ControllerBase
    {

        //ToDo implement SSE endpoint



        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }


        private async Task ListenToEventsAndSendNotificationsAsync(string clientId, CancellationToken cancellationToken)
        {
            Guid subscriptionId = Guid.Empty;

            try
            {
                // Subscribe to the notification queue
                var (subId, reader) = NotificationQueue.Subscribe(clientId);
                subscriptionId = subId;

                logger.LogInformation("SSE subscription created: SubscriptionId={SubscriptionId}, ClientId={ClientId}", subscriptionId, clientId);

                // Main loop for reading notifications and sending keep-alives
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Attendre un événement ou timeout pour keep-alive
                    var waitTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                    
                    if (await waitTask)
                    {
                        // Read all available notifications
                        while (reader.TryRead(out var notification))
                        {
                            await WriteNotificationToResponseAsync(clientId, notification.EventName, notification, cancellationToken);
                        }
                    }
                    else
                    {
                        // closed channel
                        logger.LogInformation("SSE channel closed for ClientId={ClientId}", clientId);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("SSE connection cancelled: ClientId={ClientId}", clientId);
            }
            catch (Exception ex)
            {
                await WriteNotificationToResponseAsync(clientId, "Error", "", cancellationToken);
                logger.LogError(ex, "SSE error for ClientId={ClientId}", clientId);
            }
            finally
            {
                if (subscriptionId != Guid.Empty)
                {
                    NotificationQueue.Unsubscribe(clientId, subscriptionId);
                    logger.LogInformation("SSE connection closed: ClientId={ClientId}, SubscriptionId={SubscriptionId}", clientId, subscriptionId);
                }
            }
        }

        private async Task WriteNotificationToResponseAsync(string clientId, string eventName, object payload, CancellationToken cancellationToken)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                logger.LogDebug("SSE event sent: {EventName} to ClientId={ClientId}", eventName, clientId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while building response with notifications");
            }
        }
    }
}
