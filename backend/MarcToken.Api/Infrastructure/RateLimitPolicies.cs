using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MarcToken.Api.Infrastructure;

/// <summary>
/// Hassas endpoint'ler için istek sınırları. Kimliği doğrulanmış isteklerde kullanıcı,
/// diğerlerinde istemci IP'si (Caddy'nin X-Forwarded-For başlığından) bölüm anahtarıdır.
/// </summary>
public static class RateLimitPolicies
{
    public const string JobCreate = "job-create";
    public const string IapVerify = "iap-verify";
    public const string Support = "support";
    public const string IapWebhook = "iap-webhook";

    public static IServiceCollection AddMarcRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Çok fazla istek gönderdiniz. Lütfen biraz bekleyip tekrar deneyin." }, ct);
            };

            // İş oluşturma: dakikada 10
            options.AddPolicy(JobCreate, context => FixedWindow(context, permitLimit: 10, window: TimeSpan.FromMinutes(1)));
            // IAP doğrulama: mobil istemci açılışta bekleyen satın almaları topluca yeniden doğrulayabilir
            options.AddPolicy(IapVerify, context => FixedWindow(context, permitLimit: 30, window: TimeSpan.FromMinutes(1)));
            // Destek talebi: 10 dakikada 5
            options.AddPolicy(Support, context => FixedWindow(context, permitLimit: 5, window: TimeSpan.FromMinutes(10)));
            // Mağaza iade bildirimleri (anonim): her bildirim mağaza API'sine istek attığı için IP başına sınırlı
            options.AddPolicy(IapWebhook, context => FixedWindow(context, permitLimit: 60, window: TimeSpan.FromMinutes(1)));
        });
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permitLimit, TimeSpan window)
    {
        return RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
        });
    }

    private static string PartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                return "user:" + UserContext.GetUserIdOrThrow(context.User);
            }
            catch (InvalidOperationException)
            {
                // Kullanıcı kimliği yoksa IP'ye düş
            }
        }
        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
}
