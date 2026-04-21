using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Notifications;
using System.Security.Claims;

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
            if (NotificationQueue.PublishToClient(username, new QueuedNotification("SendMessageToUser", message, null)))
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
            if (NotificationQueue.PublishToClient(username, new QueuedNotification("BroadcastMessage", message, null)))
            {
                return Ok();
            }
            logger.LogWarning("Broadcast message: {Message} from user: {Username} failed", message, username);
            return BadRequest("Failed to broadcast message. No users may be connected.");
        }

        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }
    }
}
