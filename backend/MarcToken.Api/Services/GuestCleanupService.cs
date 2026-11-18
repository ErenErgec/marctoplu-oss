using System.Security.Cryptography;
using System.Text;
using FirebaseAdmin.Auth;
using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

/// <summary>
/// Belirli bir süreden (varsayılan 3 gün) daha eski olan ve kalıcı hesaba dönüştürülmemiş
/// misafir (anonim) kullanıcıları Firebase Auth ve yerel veritabanından temizleyen arka plan servisi.
/// </summary>
public sealed class GuestCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<AppOptions> options,
    ILogger<GuestCleanupService> logger) : BackgroundService
{
    private readonly AppOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Sunucu ayağa kalktıktan sonra ilk tarama için 2 dakika bekle
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Günde bir kez çalıştır
        var interval = TimeSpan.FromHours(24);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanUpExpiredGuestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Misafir kullanıcı temizleme işlemi sırasında beklenmeyen hata oluştu.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanUpExpiredGuestsAsync(CancellationToken ct)
    {
        var retentionDays = _options.GuestRetentionDays;
        if (retentionDays <= 0)
        {
            logger.LogInformation("GuestRetentionDays <= 0 olduğundan misafir temizleme servisi pasif.");
            return;
        }

        FirebaseAuth? auth;
        try
        {
            auth = FirebaseAuth.DefaultInstance;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase Admin SDK örneği alınamadı. Misafir temizleme atlanıyor.");
            return;
        }

        if (auth == null)
        {
            logger.LogWarning("FirebaseAuth.DefaultInstance boş (null). Firebase başlatılamamış olabilir.");
            return;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        logger.LogInformation("Misafir kullanıcı temizleme başladı. Eşik tarihi: {CutoffDate:u} (Son {Days} gün)", cutoffDate, retentionDays);

        var deletedCount = 0;
        var pagedEnumerable = auth.ListUsersAsync(null);

        await foreach (var user in pagedEnumerable.WithCancellation(ct))
        {
            try
            {
                // GÜVENLİK KONTROLLERİ:
                // 1. Asla e-posta veya telefon numarası olan kullanıcıları silme (kalıcı hesap).
                if (!string.IsNullOrWhiteSpace(user.Email) || !string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    continue;
                }

                // 2. Kullanıcıya herhangi bir kimlik sağlayıcı (Google, Apple, Password vs.) bağlıysa silme.
                if (user.ProviderData != null && user.ProviderData.Length > 0)
                {
                    continue;
                }

                // 3. Kullanıcının oluşturulma veya son giriş tarihini kontrol et.
                var createdAt = user.UserMetaData?.CreationTimestamp;
                var lastSignInAt = user.UserMetaData?.LastSignInTimestamp;

                var mostRecentActivity = lastSignInAt ?? createdAt;
                if (!mostRecentActivity.HasValue || mostRecentActivity.Value >= cutoffDate)
                {
                    // Kullanıcı henüz 3 günü doldurmamış veya son 3 günde aktif olmuş.
                    continue;
                }

                logger.LogInformation("Süresi dolan misafir hesabı temizleniyor: UID={Uid}, Oluşturulma={CreatedAt:u}, Son Giriş={LastSignIn:u}",
                    user.Uid, createdAt, lastSignInAt);

                // 4. Firebase Authentication'dan kullanıcıyı sil
                await auth.DeleteUserAsync(user.Uid, ct);

                // 5. Yerel veritabanında (PostgreSQL) ilişkili kayıtları temizle / anonimleştir
                var raw = user.Uid;
                Guid userId;
                if (!Guid.TryParse(raw, out userId))
                {
                    var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
                    userId = new Guid(hash);
                }

                using (var scope = scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // İş geçmişini anonimleştir (bağlantıyı kopar)
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

                    // Varsa kullanıcı profilini sil
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
                }

                deletedCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Kullanıcı {Uid} temizlenirken hata oluştu.", user.Uid);
            }
        }

        if (deletedCount > 0)
        {
            logger.LogInformation("Misafir kullanıcı temizleme tamamlandı: {Count} adet süresi dolan hesap silindi.", deletedCount);
        }
        else
        {
            logger.LogInformation("Temizlenecek süresi dolmuş misafir kullanıcı bulunamadı.");
        }
    }
}
