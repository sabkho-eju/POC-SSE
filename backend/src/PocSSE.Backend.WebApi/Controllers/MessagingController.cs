using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Responses;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;

namespace PocSSE.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagingController(NotificationQueue NotificationQueue,
        ILogger<MessagingController> logger) : ControllerBase
    {
        [HttpPost("send-message-to-a-user")]
        [Authorize]
        public IActionResult SendNotificationToUser(string recipientClientId, string message)
        {
            var username = GetAuthenticatedUsername();
            logger.LogInformation("Send message: {Message} from user: {Username} to recipient : {RecipientId}", message,
                username, recipientClientId);

            var sendMessageData = JsonSerializer.SerializeToElement(new MessagingNotification(message));
            if (NotificationQueue.PublishToClient(recipientClientId, new QueuedNotification("SendMessageToUser", sendMessageData)))
            {
                return Ok();
            }
            logger.LogWarning("Send message: {Message} from user: {Username} to recipient : {RecipientId} failed", message,
                username, recipientClientId);
            return BadRequest("Failed to send message. Recipient may not be connected.");
        }

        [HttpPost("broadcast-to-all-users")]
        [Authorize]
        public IActionResult BroadcastNotificationToAllUsers(string message)
        {
            var username = GetAuthenticatedUsername();
            logger.LogInformation("Broadcast message: {Message} from user: {Username}", message, username);
            var broadcastMessageData = JsonSerializer.SerializeToElement(new MessagingNotification(message));
            if (NotificationQueue.PublishToClient(username, new QueuedNotification("BroadcastMessage", broadcastMessageData)))
            {
                return Ok();
            }
            logger.LogWarning("Broadcast message: {Message} from user: {Username} failed", message, username);
            return BadRequest("Failed to broadcast message. No users may be connected.");
        }

        [HttpGet("messaging-notification-stream")]
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

                return Results.ServerSentEvents(notificationStream, eventType: "MessagingNotification");
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

        private async IAsyncEnumerable<MessagingNotification> Notifications(string userName, ChannelReader<QueuedNotification> channelReader, CancellationToken cancellationToken)
        {
            await foreach (var notification in channelReader.ReadAllAsync(cancellationToken))
            {
                logger.LogInformation("Sending messaging notification to client {Username}: {Notification}", userName, JsonSerializer.SerializeToElement(notification).ToString());
                yield return MapToMessagingNotification(notification);
            }
        }

        private MessagingNotification MapToMessagingNotification(QueuedNotification queuedNotification)
        {
            var message = string.Empty;

            if (queuedNotification.Data.HasValue)
            {
                var data = queuedNotification.Data.Value;

                // Extraire message (case-insensitive)
                if (data.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString() ?? string.Empty;
                }
                else if (data.TryGetProperty("Message", out var messageElementCaps))
                {
                    message = messageElementCaps.GetString() ?? string.Empty;
                }
            }

            return new MessagingNotification(message);
        }


        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }
    }
}
