using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PocSSE.Backend.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobProcessingController(ILogger<JobProcessingController> logger) : ControllerBase
{
    [HttpGet("test")]
    [Authorize]
    public IActionResult Test()
    {
        return Ok("Controller is working!");
    }

    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessJob([FromBody] JobRequest request)
    {
        logger.LogInformation("Processing job: {JobId}", request.JobId);
        
        await Task.Delay(100); // Simulation

        return Ok(new JobResponse
        {
            JobId = request.JobId,
            Status = "Completed",
            ProcessedAt = DateTime.UtcNow
        });
    }
}

public record JobRequest(string JobId, string JobData);
public record JobResponse
{
    public required string JobId { get; init; }
    public required string Status { get; init; }
    public DateTime ProcessedAt { get; init; }
}