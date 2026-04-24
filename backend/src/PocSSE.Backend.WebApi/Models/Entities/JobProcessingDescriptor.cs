namespace PocSSE.Backend.WebApi.Models.Entities
{
    public sealed record JobProcessingDescriptor(string ClientId, string JobId, string Description, int DurationSeconds);
}