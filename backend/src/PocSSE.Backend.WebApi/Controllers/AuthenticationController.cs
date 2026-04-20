using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Models;
using PocSSE.Backend.WebApi.Models.API.Requests;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Services;

namespace PocSSE.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(AuthenticationService authService) : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var (success, token) = authService.ValidateAndGenerateToken(request.Username, request.Password);

            if (!success)
            {
                return Ok(new LoginResponse(
                    Success: false,
                    Token: null,
                    Username: null,
                    Message: "Invalid username or password"
                ));
            }

            return Ok(new LoginResponse(
                Success: true,
                Token: token,
                Username: request.Username,
                Message: "Login successful"
            ));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Pour JWT, le logout est géré côté client (suppression du token)
            return Ok(new { Success = true, Message = "Logged out successfully" });
        }
    }
}
