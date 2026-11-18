using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Services;

public enum IapRefundOutcome
{
    /// <summary>İade işlendi, tokenler düşüldü.</summary>
    Refunded,
    /// <summary>Daha önce işlenmiş iade geri alındı, tokenler geri yüklendi.</summary>
    RefundReversed,
    /// <summary>Durum zaten güncel (iade daha önce işlenmiş veya iade yok).</summary>
    NoChange,
    /// <summary>Satın alma bizde kredilendirilmemiş veya mağaza tanımıyor; yapılacak bir şey yok.</summary>
    UnknownPurchase,
    /// <summary>Mağazaya ulaşılamadı veya mağaza bildirimi henüz yansıtmıyor; bildirim tekrar gönderilmeli.</summary>
    RetryLater,
}

/// <summary>
/// Mağaza iade bildirimlerini işler. Bildirim yalnızca tetikleyicidir: karar her zaman mağazanın kimliği
/// doğrulanmış API'sinden okunan güncel duruma göre verilir, bu yüzden sahte bir bildirim token hareketine yol açamaz.
/// Ledger reason'ları: satın alma "iap:&lt;platform&gt;:&lt;ref&gt;", iade "iap_refund:&lt;platform&gt;:&lt;ref&gt;" (negatif),
/// iadenin geri alınması "iap_refund_reversed:&lt;platform&gt;:&lt;ref&gt;" (pozitif). Hepsi bir kez yazılır.
/// </summary>
public sealed class IapRefundService(
    AppDbContext db,
    TokenService tokenService,
    GooglePlayPurchaseVerifier googlePlayVerifier,
    AppStorePurchaseVerifier appStoreVerifier,
    ILogger<IapRefundService> logger)
{
    public const string RefundReasonPrefix = "iap_refund:";
    public const string RefundReversedReasonPrefix = "iap_refund_reversed:";

    /// <param name="platform">"android" veya "ios".</param>
    /// <param name="storeReference">Android purchaseToken, iOS transactionId (satın alma ledger reason'ındaki kimlik).</param>
    /// <param name="expectRefunded">
    /// Bildirim iade bildiriyorsa true. Mağaza API'si iadeyi henüz göstermiyorsa bildirim tekrar denensin diye RetryLater döner.
    /// </param>
    /// <param name="source">Ledger metadata'sına yazılan bildirim kaynağı (google_rtdn, apple_notification).</param>
    public async Task<IapRefundOutcome> ProcessAsync(
        string platform,
        string storeReference,
        bool expectRefunded,
        string source,
        CancellationToken ct)
    {
        var key = $"{platform}:{storeReference}";
        var purchase = await db.TokenLedgers
            .AsNoTracking()
            .Where(x => x.Reason == "iap:" + key && x.Delta > 0)
            .Select(x => new { x.Id, x.UserId, x.Delta })
            .FirstOrDefaultAsync(ct);

        if (purchase is null)
        {
            logger.LogInformation("İade bildirimi kredilendirilmemiş bir satın alma için geldi, yok sayıldı. Platform: {Platform}", platform);
            return IapRefundOutcome.UnknownPurchase;
        }

        // Hesap silinince ledger satırları anonimleştirilir (UserId = Guid.Empty); düşülecek bir cüzdan kalmaz.
        if (purchase.UserId == Guid.Empty)
        {
            logger.LogWarning("İade bildirimi silinmiş bir hesabın satın alması için geldi; token düşülemedi. Platform: {Platform}", platform);
            return IapRefundOutcome.UnknownPurchase;
        }

        var check = platform == "android"
            ? await googlePlayVerifier.CheckRefundAsync(storeReference, ct)
            : await appStoreVerifier.CheckRefundAsync(storeReference, ct);

        var metadata = new Dictionary<string, object?>(check.Metadata ?? new Dictionary<string, object?>())
        {
            ["platform"] = platform,
            ["source"] = source,
            ["purchaseLedgerId"] = purchase.Id,
        };

        switch (check.Status)
        {
            case IapRefundStatus.NotConfigured:
            case IapRefundStatus.StoreUnavailable:
                logger.LogWarning("İade bildirimi şu an doğrulanamadı ({Status}), mağaza tekrar gönderecek. Platform: {Platform}, Kullanıcı: {UserId}",
                    check.Status, platform, purchase.UserId);
                return IapRefundOutcome.RetryLater;

            case IapRefundStatus.NotFound:
                logger.LogWarning("İade bildirimi: mağaza satın almayı tanımadı. Platform: {Platform}, Kullanıcı: {UserId}", platform, purchase.UserId);
                return IapRefundOutcome.UnknownPurchase;

            case IapRefundStatus.Refunded:
                if (!await tokenService.DebitOnceAsync(purchase.UserId, purchase.Delta, RefundReasonPrefix + key, metadata, ct))
                {
                    return IapRefundOutcome.NoChange;
                }

                logger.LogWarning("IAP iadesi işlendi: {Tokens} token kullanıcıdan ({UserId}) düşüldü. Platform: {Platform}",
                    purchase.Delta, purchase.UserId, platform);
                return IapRefundOutcome.Refunded;

            default:
                if (expectRefunded)
                {
                    logger.LogWarning("İade bildirimi geldi ama mağaza API'si iadeyi henüz göstermiyor; tekrar denenecek. Platform: {Platform}, Kullanıcı: {UserId}",
                        platform, purchase.UserId);
                    return IapRefundOutcome.RetryLater;
                }

                // Mağaza satın almayı yeniden geçerli sayıyor: daha önce iade işlendiyse tokenleri geri yükle.
                if (!await db.TokenLedgers.AnyAsync(x => x.Reason == RefundReasonPrefix + key, ct)
                    || !await tokenService.CreditOnceAsync(purchase.UserId, purchase.Delta, RefundReversedReasonPrefix + key, metadata, ct))
                {
                    return IapRefundOutcome.NoChange;
                }

                logger.LogInformation("IAP iadesi geri alındı: {Tokens} token kullanıcıya ({UserId}) geri yüklendi. Platform: {Platform}",
                    purchase.Delta, purchase.UserId, platform);
                return IapRefundOutcome.RefundReversed;
        }
    }
}
