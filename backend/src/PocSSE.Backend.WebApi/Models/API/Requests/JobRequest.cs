namespace PocSSE.Backend.WebApi.Models.API.Requests
{
    public record JobRequest(string JobId, string JobData, int DurationSeconds = 5);
}
