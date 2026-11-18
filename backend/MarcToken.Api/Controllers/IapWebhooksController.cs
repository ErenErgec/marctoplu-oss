using System.Text;
using System.Text.Json;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Controllers;

/// <summary>
/// Mağaza sunucu bildirimleri (iade). Kimlik doğrulaması yoktur: bildirim yalnızca tetikleyicidir ve
/// IapRefundService kararı mağazanın kimliği doğrulanmış API'sine sorarak verir.
/// 2xx dışındaki yanıtlarda mağaza bildirimi tekrar gönderir (Pub/Sub üstel geri çekilme, Apple 5 deneme).
/// </summary>
[ApiController]
[Route("api/payments/iap/webhooks")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.IapWebhook)]
public sealed class IapWebhooksController(
    IapRefundService refundService,
    IOptions<IapOptions> options,
    ILogger<IapWebhooksController> logger) : ControllerBase
{
    private readonly IapOptions _options = options.Value;

    /// <summary>
    /// Google Play Real-time Developer Notifications (Cloud Pub/Sub push aboneliği).
    /// Zarf: { message: { data: base64(DeveloperNotification JSON), messageId }, subscription }.
    /// Yalnızca tek seferlik ürünlerin voidedPurchaseNotification bildirimi işlenir.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] JsonElement body, CancellationToken ct)
    {
        var data = body.ValueKind == JsonValueKind.Object
                   && body.TryGetProperty("message", out var message)
                   && message.TryGetProperty("data", out var dataNode)
                   && dataNode.ValueKind == JsonValueKind.String
            ? dataNode.GetString()
            : null;

        JsonDocument notification;
        try
        {
            notification = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(data ?? "")));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return BadRequest(new { message = "Geçersiz Pub/Sub mesajı." });
        }

        using (notification)
        {
            var root = notification.RootElement;
            var packageName = ReadString(root, "packageName");
            if (!string.Equals(packageName, _options.GooglePlayPackageName, StringComparison.Ordinal))
            {
                logger.LogWarning("Google Play bildirimi başka bir paket için geldi, yok sayıldı: {PackageName}", packageName);
                return Ok();
            }

            if (root.TryGetProperty("testNotification", out _))
            {
                logger.LogInformation("Google Play test bildirimi alındı (RTDN bağlantısı çalışıyor).");
                return Ok();
            }

            // productType: 1 abonelik, 2 tek seferlik ürün. Diğer bildirim türleri (satın alma vb.) bu uç noktada işlenmez.
            if (!root.TryGetProperty("voidedPurchaseNotification", out var voided)
                || (voided.TryGetProperty("productType", out var productType) && productType.TryGetInt32(out var type) && type != 2))
            {
                return Ok();
            }

            var purchaseToken = ReadString(voided, "purchaseToken");
            if (string.IsNullOrWhiteSpace(purchaseToken))
            {
                return BadRequest(new { message = "purchaseToken eksik." });
            }

            var outcome = await refundService.ProcessAsync("android", purchaseToken, expectRefunded: true, "google_rtdn", ct);
            return ToResponse(outcome);
        }
    }

    /// <summary>
    /// App Store Server Notifications V2: { signedPayload }. REFUND ve REFUND_REVERSED işlenir;
    /// diğer türler (CONSUMPTION_REQUEST, REFUND_DECLINED, TEST…) yalnızca loglanır.
    /// İmza zinciri doğrulanmaz; işlem durumu App Store Server API'den ayrıca okunur.
    /// </summary>
    [HttpPost("apple")]
    public async Task<IActionResult> Apple([FromBody] JsonElement body, CancellationToken ct)
    {
        using var payload = JwsPayload.TryParse(
            body.ValueKind == JsonValueKind.Object ? ReadString(body, "signedPayload") : null);
        if (payload is null)
        {
            return BadRequest(new { message = "Geçersiz signedPayload." });
        }

        var root = payload.RootElement;
        var notificationType = ReadString(root, "notificationType");
        if (notificationType is not ("REFUND" or "REFUND_REVERSED"))
        {
            logger.LogInformation("App Store bildirimi alındı: {Type}/{Subtype}", notificationType, ReadString(root, "subtype"));
            return Ok();
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new { message = "Bildirimde data yok." });
        }

        var bundleId = ReadString(data, "bundleId");
        if (!string.Equals(bundleId, _options.AppleBundleId, StringComparison.Ordinal))
        {
            logger.LogWarning("App Store bildirimi başka bir uygulama için geldi, yok sayıldı: {BundleId}", bundleId);
            return Ok();
        }

        using var transaction = JwsPayload.TryParse(ReadString(data, "signedTransactionInfo"));
        var transactionId = transaction is null ? null : ReadString(transaction.RootElement, "transactionId");
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return BadRequest(new { message = "signedTransactionInfo okunamadı." });
        }

        var outcome = await refundService.ProcessAsync(
            "ios", transactionId, expectRefunded: notificationType == "REFUND", "apple_notification", ct);
        return ToResponse(outcome);
    }

    // Sonuç yanıtta açıklanmaz: bir satın alma kimliğinin bizde kayıtlı olup olmadığı dışarıdan sorgulanamasın.
    private IActionResult ToResponse(IapRefundOutcome outcome) => outcome == IapRefundOutcome.RetryLater
        ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Daha sonra tekrar deneyin." })
        : Ok();

    private static string? ReadString(JsonElement parent, string name)
        => parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
