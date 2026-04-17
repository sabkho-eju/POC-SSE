namespace PocSSE.Backend.WebApi.Models.API.Requests
{
    public record SendNotificationRequest(
        string ClientId,
        string EventName,
        string JobId,
        string Message,
        string Status
    );
}
