namespace PocSSE.Backend.WebApi.Models
{
    public record LoginResponse(bool Success, string? Token, string? Username, string? Message);
}
