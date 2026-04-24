using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Notifications;
using PocSSE.Backend.WebApi.Models.API.Responses;
using System.Security.Claims;
using System.Text.Json;

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

            var sendMessageData = JsonSerializer.SerializeToElement(new MessagingNotification("SendMessageToUser", message));
            var count = NotificationQueue.Publish(recipientClientId, new QueuedNotification("MessagingNotification", sendMessageData));

            if (count > 0)
            {
                return Ok();
            }
            logger.LogWarning("Send message: {Message} from user: {Username} to recipient : {RecipientId} failed", message,
                username, recipientClientId);
            return BadRequest("Failed to send message. No subscribers may be listening.");
        }

        [HttpPost("broadcast-to-all-users")]
        [Authorize]
        public IActionResult BroadcastNotificationToAllUsers(string message)
        {
            var username = GetAuthenticatedUsername();
            logger.LogInformation("Broadcast message: {Message} from user: {Username}", message, username);
            var broadcastMessageData = JsonSerializer.SerializeToElement(new MessagingNotification("BroadcastMessage", message));
            var count = NotificationQueue.Publish(null, new QueuedNotification("MessagingNotification", broadcastMessageData));

            if (count > 0)
            {
                return Ok();
            }
            logger.LogWarning("Broadcast message: {Message} from user: {Username} failed", message, username);
            return BadRequest("Failed to broadcast message. No users may be listening.");
        }

        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }
    }
}
