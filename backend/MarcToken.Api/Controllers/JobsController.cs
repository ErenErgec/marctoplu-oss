using MarcToken.Api.Contracts;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class JobsController(JobService jobService) : ControllerBase
{
    [HttpPost("upload")]
    [EnableRateLimiting(RateLimitPolicies.JobCreate)]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<JobCreateResponse>> Create([FromForm] IFormFile file, CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        try
        {
            var job = await jobService.CreateJobAsync(userId, file, ct);
            return Ok(new JobCreateResponse(job.Id, job.Status, job.EstimatedTokens));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("from-isbns")]
    [EnableRateLimiting(RateLimitPolicies.JobCreate)]
    public async Task<ActionResult<JobCreateResponse>> CreateFromIsbns([FromBody] CreateJobFromIsbnsRequest request, CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        try
        {
            var job = await jobService.CreateJobFromIsbnsAsync(userId, request.Isbns, ct);
            return Ok(new JobCreateResponse(job.Id, job.Status, job.EstimatedTokens));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<JobResponse>>> List([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        var jobs = await jobService.ListJobsAsync(userId, limit, ct);
        var result = jobs.Select(ToResponse).ToList();
        return Ok(result);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobResponse>> Get([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        var job = await jobService.GetJobAsync(userId, jobId, ct);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<ActionResult<JobResponse>> Cancel([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        var job = await jobService.CancelJobAsync(userId, jobId, ct);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    [HttpDelete("{jobId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        try
        {
            var deleted = await jobService.DeleteJobAsync(userId, jobId, ct);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid jobId, [FromQuery] string format = "xlsx", CancellationToken ct = default)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        try
        {
            var result = await jobService.GetJobDownloadAsync(userId, jobId, format, ct);
            if (result is null)
            {
                return NotFound();
            }

            Response.Headers["Access-Control-Expose-Headers"] = "X-Found-Count, X-Not-Found-Count, X-Total-Count";
            Response.Headers["X-Found-Count"] = result.FoundCount.ToString();
            Response.Headers["X-Not-Found-Count"] = result.NotFoundCount.ToString();
            Response.Headers["X-Total-Count"] = result.Job.TotalIsbns.ToString();

            var stream = System.IO.File.OpenRead(result.FilePath);
            return File(stream, result.ContentType, result.DownloadFileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static JobResponse ToResponse(Data.Job job)
        => new(
            job.Id,
            job.Status,
            job.TotalIsbns,
            job.ProcessedIsbns,
            job.ConsumedTokens,
            job.RefundedTokens,
            job.ErrorMessage,
            !string.IsNullOrWhiteSpace(job.OutputFilePath),
            job.CreatedAt,
            job.UpdatedAt
        );
}
