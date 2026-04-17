namespace PocSSE.Backend.WebApi.Models.API.Responses
{
    public record LoginResponse(bool Success, string? Token, string? Username, string? Message);
}
