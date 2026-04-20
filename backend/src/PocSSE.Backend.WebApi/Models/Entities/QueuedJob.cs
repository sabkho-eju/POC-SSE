namespace PocSSE.Backend.WebApi.Models.Entities
{
    public sealed record QueuedJob(string ClientId, string JobId, string Description, int DurationSeconds);
}
