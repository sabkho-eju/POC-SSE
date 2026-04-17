namespace PocSSE.Backend.WebApi.Models.API.Responses
{
    public record JobResponse
    {
        public required string JobId { get; init; }
        public required string Status { get; init; }
        public DateTime ProcessedAt { get; init; }
    }
}
