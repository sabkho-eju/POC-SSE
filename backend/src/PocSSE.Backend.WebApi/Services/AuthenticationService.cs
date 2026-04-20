using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace PocSSE.Backend.WebApi.Services
{
    public class AuthenticationService(IConfiguration configuration)
    {
        // Utilisateurs hardcodés pour le POC
        private readonly Dictionary<string, string> _users = new()
        {
            { "admin", "password123" },
            { "user1", "pass123" },
            { "testuser", "testpassword" },
            { "demo", "demo" }
        };

        public (bool Success, string? Token) ValidateAndGenerateToken(string username, string password)
        {
            // Valider les credentials
            if (!_users.TryGetValue(username, out var storedPassword) || storedPassword != password)
            {
                return (false, null);
            }

            // Générer le token JWT
            var token = GenerateJwtToken(username);
            return (true, token);
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ??
                            throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "POC-SSE-Backend";
            var audience = jwtSettings["Audience"] ?? "POC-SSE-Frontend";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
