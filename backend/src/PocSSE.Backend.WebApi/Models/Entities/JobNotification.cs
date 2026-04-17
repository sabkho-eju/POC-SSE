namespace PocSSE.Backend.WebApi.Models.Entities
{
    public sealed record JobNotification(string EventName, string Message, string Status, string JobId, DateTimeOffset? CompletedAt = null);
}
