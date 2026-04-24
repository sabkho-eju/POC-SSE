using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocSSE.Backend.WebApi.Infra.Jobs;
using PocSSE.Backend.WebApi.Models.API.Requests;
using PocSSE.Backend.WebApi.Models.API.Responses;
using PocSSE.Backend.WebApi.Models.Entities;
using System.Security.Claims;

namespace PocSSE.Backend.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobProcessingController(
    BackgroundJobQueue backgroundJobQueue,
    ILogger<JobProcessingController> logger) : ControllerBase
{
    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessJob([FromBody] JobRequest request)
    {
        var username = GetAuthenticatedUsername();

        logger.LogInformation("Processing job: {JobId} for user: {Username}", request.JobId, username);

        await backgroundJobQueue.QueueAsync(new QueuedJob(
            JobId: request.JobId,
            ClientId: username,
            Description: request.JobData,
            DurationSeconds: request.DurationSeconds));

        return Ok(new JobResponse(request.JobId, "JobQueued", DateTime.UtcNow));
    }

    [HttpPost("cancel")]
    [Authorize]
    public IActionResult Cancel(string jobId)
    {
        var username = GetAuthenticatedUsername();
        logger.LogInformation("Cancelling job: {JobId} for user: {Username}", jobId, username);

        //ToDo : implement action

        return Ok($"Cancelled {jobId}");
    }

    private string GetAuthenticatedUsername()
    {
        return User.Identity?.Name
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? throw new UnauthorizedAccessException("User identity not found");
    }
}

