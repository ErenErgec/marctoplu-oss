using MarcToken.Api.Contracts;
using MarcToken.Api.Data;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/payments/iap")]
[Authorize]
public sealed class InAppPurchasesController(
    TokenService tokenService,
    GooglePlayPurchaseVerifier googlePlayVerifier,
    AppStorePurchaseVerifier appStoreVerifier,
    AppDbContext db,
    ILogger<InAppPurchasesController> logger) : ControllerBase
{
    private static readonly Dictionary<string, int> ProductTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        { "marc_tokens_100", 100 },
        { "marc_tokens_500", 500 },
        { "marc_tokens_2000", 2000 },
    };

    private const string RetryLaterMessage =
        "Satın alma şu anda doğrulanamıyor. Ödemeniz güvende; uygulama satın almayı daha sonra otomatik olarak tekrar doğrulayacak.";

    [HttpPost("verify-receipt")]
    [EnableRateLimiting(RateLimitPolicies.IapVerify)]
    public async Task<IActionResult> VerifyReceipt([FromBody] VerifyIapReceiptRequest request, CancellationToken ct)
    {
        if (UserContext.IsAnonymous(User))
        {
            return BadRequest(new
            {
                message = "Uygulama içi satın alma yapabilmek için lütfen önce hesabınızı e-posta veya Google ile kalıcı bir hesaba dönüştürün."
            });
        }

        var userId = UserContext.GetUserIdOrThrow(User);

        if (!ProductTokens.TryGetValue(request.ProductId, out var tokenAmount) || tokenAmount <= 0)
        {
            logger.LogWarning("Geçersiz IAP ürün kimliği: {ProductId} - Kullanıcı: {UserId}", request.ProductId, userId);
            return BadRequest(new { message = $"Tanımsız ürün kimliği: {request.ProductId}" });
        }

        var platform = request.Platform.Trim().ToLowerInvariant();
        if (platform is not ("android" or "ios"))
        {
            return BadRequest(new { message = $"Desteklenmeyen platform: {request.Platform}" });
        }

        // Android: purchaseToken, iOS: App Store transactionId mağazadaki tekil satın alma kimliğidir.
        var storeReference = (platform == "ios" ? request.TransactionId : request.PurchaseToken)?.Trim();
        if (string.IsNullOrWhiteSpace(storeReference))
        {
            return BadRequest(new
            {
                message = platform == "ios"
                    ? "Satın alma işlem kimliği (transactionId) eksik."
                    : "Satın alma doğrulama anahtarı (purchaseToken) eksik."
            });
        }

        // İstemci tekrar denediğinde mağazayı boşuna sorgulamamak için ön kontrol.
        // Asıl mükerrer koruma CreditOnceAsync içindeki kilitli kontroldür.
        if (await db.TokenLedgers.AnyAsync(x => x.Reason == $"iap:{platform}:{storeReference}", ct))
        {
            return await AlreadyProcessedAsync(userId, ct);
        }

        var verification = platform == "android"
            ? await googlePlayVerifier.VerifyAsync(request.ProductId, storeReference, ct)
            : await appStoreVerifier.VerifyAsync(request.ProductId, storeReference, ct);

        switch (verification.Status)
        {
            case IapVerificationStatus.NotConfigured:
                logger.LogError("IAP doğrulaması yapılandırılmamış, satın alma reddedildi. Platform: {Platform}, Kullanıcı: {UserId}. {Error}",
                    platform, userId, verification.Error);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = RetryLaterMessage });

            case IapVerificationStatus.StoreUnavailable:
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = RetryLaterMessage });

            case IapVerificationStatus.Invalid:
                logger.LogWarning("IAP doğrulaması başarısız. Platform: {Platform}, Ürün: {ProductId}, Kullanıcı: {UserId}. {Error}",
                    platform, request.ProductId, userId, verification.Error);
                return BadRequest(new { message = $"Satın alma doğrulanamadı: {verification.Error}" });
        }

        var reason = $"iap:{platform}:{verification.TransactionKey}";
        var credited = await tokenService.CreditOnceAsync(userId, tokenAmount, reason, verification.Metadata, ct);
        if (!credited)
        {
            return await AlreadyProcessedAsync(userId, ct);
        }

        var updatedBalance = await tokenService.GetBalanceAsync(userId, ct: ct);

        logger.LogInformation("IAP Satın alma doğrulandı: {Tokens} token kullanıcıya ({UserId}) yüklendi. Platform: {Platform}, Ürün: {ProductId}",
            tokenAmount, userId, platform, request.ProductId);

        return Ok(new
        {
            success = true,
            message = $"{tokenAmount} MARC Token hesabınıza başarıyla yüklendi!",
            balance = updatedBalance,
            creditedTokens = tokenAmount
        });
    }

    private async Task<IActionResult> AlreadyProcessedAsync(Guid userId, CancellationToken ct)
    {
        var currentBalance = await tokenService.GetBalanceAsync(userId, ct: ct);
        return Ok(new
        {
            success = true,
            message = "Bu satın alma işlemi daha önce hesabınıza tanımlanmış.",
            balance = currentBalance,
            creditedTokens = 0
        });
    }
}
