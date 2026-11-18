using MarcToken.Api.Contracts;
using MarcToken.Api.Data;
using MarcToken.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/me/profile")]
[Authorize]
public sealed class ProfileController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> Get(CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        var email = UserContext.GetEmail(User);

        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (!string.IsNullOrWhiteSpace(email) && !UserContext.IsAnonymous(User))
        {
            if (profile is null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    Email = email,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync(ct);
            }
            else if (profile.Email != email)
            {
                profile.Email = email;
                await db.SaveChangesAsync(ct);
            }
        }

        return Ok(new UserProfileResponse(
            email,
            profile?.Name,
            profile?.Surname,
            profile?.IdentityNumber,
            profile?.GsmNumber,
            profile?.City,
            profile?.Country,
            profile?.Address,
            profile?.ZipCode,
            profile?.UpdatedAt
        ));
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> Update(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        if (UserContext.IsAnonymous(User))
        {
            return BadRequest(new { message = "Profil bilgisi kaydedebilmek için lütfen önce hesabınızı e-posta veya Google ile kalıcı bir hesaba dönüştürün." });
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
        {
            return BadRequest(new { message = "Ad alanı zorunludur ve en az 2 karakter olmalıdır." });
        }

        if (string.IsNullOrWhiteSpace(request.Surname) || request.Surname.Trim().Length < 2)
        {
            return BadRequest(new { message = "Soyad alanı zorunludur ve en az 2 karakter olmalıdır." });
        }

        if (!ModelState.IsValid) return ValidationProblem();

        var userId = UserContext.GetUserIdOrThrow(User);
        var email = UserContext.GetEmail(User);
        var now = DateTimeOffset.UtcNow;

        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                Email = email,
                CreatedAt = now,
            };
            db.UserProfiles.Add(profile);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            profile.Email = email;
        }

        profile.Name = request.Name?.Trim();
        profile.Surname = request.Surname?.Trim();
        profile.IdentityNumber = request.IdentityNumber?.Trim();
        profile.GsmNumber = request.GsmNumber?.Trim();
        profile.City = request.City?.Trim();
        profile.Country = request.Country?.Trim();
        profile.Address = request.Address?.Trim();
        profile.ZipCode = request.ZipCode?.Trim();
        profile.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return Ok(new UserProfileResponse(
            email,
            profile.Name,
            profile.Surname,
            profile.IdentityNumber,
            profile.GsmNumber,
            profile.City,
            profile.Country,
            profile.Address,
            profile.ZipCode,
            profile.UpdatedAt
        ));
    }

    [HttpPost("fcm-token")]
    public async Task<IActionResult> UpdateFcmToken(
        [FromBody] UpdateFcmTokenRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FcmToken))
        {
            return BadRequest("FCM token cannot be empty.");
        }

        var userId = UserContext.GetUserIdOrThrow(User);
        var now = DateTimeOffset.UtcNow;

        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                CreatedAt = now,
            };
            db.UserProfiles.Add(profile);
        }

        profile.FcmToken = request.FcmToken;
        profile.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Ok();
    }

    /// <summary>
    /// Kullanıcı hesabını ve ilişkili tüm kişisel verileri siler.
    /// Apple App Store (Guideline 5.1.1(v)) ve Google Play Data Safety gereksinimi.
    /// </summary>
    [HttpDelete("/api/me/account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);

        // İş geçmişini anonimleştir (KVKK/GDPR: kişisel veri bağlantısını kaldır)
        var jobs = await db.Jobs.Where(x => x.UserId == userId).ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.UserId = Guid.Empty;
        }

        // Token ledger kayıtlarını anonimleştir
        var ledgerEntries = await db.TokenLedgers.Where(x => x.UserId == userId).ToListAsync(ct);
        foreach (var entry in ledgerEntries)
        {
            entry.UserId = Guid.Empty;
        }

        // Destek taleplerini sil (e-posta ve mesaj kişisel veri içerir)
        var tickets = await db.SupportTickets.Where(x => x.UserId == userId).ToListAsync(ct);
        db.SupportTickets.RemoveRange(tickets);

        // Kullanıcı profilini sil
        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is not null)
        {
            db.UserProfiles.Remove(profile);
        }

        // Cüzdanı sil
        var wallet = await db.Wallets.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (wallet is not null)
        {
            db.Wallets.Remove(wallet);
        }

        await db.SaveChangesAsync(ct);

        return Ok(new { Message = "Hesabınız ve kişisel verileriniz başarıyla silindi." });
    }
}
