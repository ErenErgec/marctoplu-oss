using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Services;

/// <summary>
/// İş bittiğinde kullanıcıya FCM push bildirimi gönderir. Hata fırlatmaz, sadece loglar.
/// </summary>
public sealed class JobNotificationSender(AppDbContext db, ILogger<JobNotificationSender> logger)
{
    public async Task TrySendAsync(Guid userId, string status, int processedCount)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                return;
            }

            var fcmToken = await db.UserProfiles
                .Where(x => x.UserId == userId)
                .Select(x => x.FcmToken)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                return;
            }

            var title = "İşlem Tamamlandı";
            var body = $"{processedCount} adet ISBN başarıyla sorgulandı.";

            if (status == "failed")
            {
                title = "İşlem Başarısız";
                body = "Sorgulama işlemi sırasında bir hata oluştu.";
            }
            else if (status == "cancelled")
            {
                title = "İşlem İptal Edildi";
                body = "Sorgulama işleminiz iptal edildi.";
            }

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                }
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send FCM push notification to user {UserId}", userId);
        }
    }
}
