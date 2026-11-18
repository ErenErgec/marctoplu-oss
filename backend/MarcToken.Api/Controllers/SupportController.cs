using MarcToken.Api.Contracts;
using MarcToken.Api.Data;
using MarcToken.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Controllers;

/// <summary>
/// Kullanıcının "Destek & Sorun Bildir" formundan gelen talepler.
/// Talepler admin panelindeki Destek sekmesinde listelenir.
/// </summary>
[ApiController]
[Route("api/support")]
[Authorize]
public sealed class SupportController(AppDbContext db, ILogger<SupportController> logger) : ControllerBase
{
    private const int MaxOpenTicketsPerUser = 10;

    [HttpPost("tickets")]
    [EnableRateLimiting(RateLimitPolicies.Support)]
    public async Task<ActionResult<SupportTicketCreatedResponse>> Create(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);

        var openCount = await db.SupportTickets.CountAsync(x => x.UserId == userId && x.Status == "open", ct);
        if (openCount >= MaxOpenTicketsPerUser)
        {
            return BadRequest(new { message = "Yanıt bekleyen çok sayıda talebiniz var. Lütfen mevcut talepleriniz incelenene kadar bekleyin." });
        }

        // İş kimliği yalnızca kullanıcının kendi işine aitse kaydedilir
        Guid? jobId = null;
        if (request.JobId is { } requestedJobId)
        {
            var ownsJob = await db.Jobs.AnyAsync(x => x.Id == requestedJobId && x.UserId == userId, ct);
            if (!ownsJob)
            {
                return BadRequest(new { message = "Belirtilen işlem bulunamadı." });
            }
            jobId = requestedJobId;
        }

        var ticket = new SupportTicket
        {
            UserId = userId,
            Email = request.Email.Trim(),
            JobId = jobId,
            Message = request.Message.Trim(),
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Destek talebi oluşturuldu: {TicketId} (kullanıcı {UserId}, iş {JobId})", ticket.Id, userId, jobId);
        return Ok(new SupportTicketCreatedResponse(ticket.Id, ticket.CreatedAt));
    }
}
