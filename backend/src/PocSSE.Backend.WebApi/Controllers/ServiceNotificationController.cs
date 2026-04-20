using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PocSSE.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceNotificationController(ILogger<ServiceNotificationController> logger) : ControllerBase
    {
        [HttpPost("send-message-to-a-user")]
        [Authorize]
        public IActionResult SendNotificationToUser(string recipientClientId, string message)
        {
            var username = GetAuthenticatedUsername();
            logger.LogInformation("Send message: {Message} from user: {Username} to recipient : {RecipientId}", message, username, recipientClientId);

            //ToDo : implement action

            return Ok();
        }

        [HttpPost("broadcast-to-all-users")]
        [Authorize]
        public IActionResult BroadcastNotificationToAllUsers(string message)
        {
            var username = GetAuthenticatedUsername();
            logger.LogInformation("Broadcast message: {Message} from user: {Username}", message, username);

            //ToDo : implement action

            return Ok();
        }

        private string GetAuthenticatedUsername()
        {
            return User.Identity?.Name
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? throw new UnauthorizedAccessException("User identity not found");
        }

    }
}
