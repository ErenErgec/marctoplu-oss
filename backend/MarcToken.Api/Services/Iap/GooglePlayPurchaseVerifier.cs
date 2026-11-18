using System.Net;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

/// <summary>
/// Google Play Developer API (purchases.products.get) ile tek seferlik ürün satın almasını doğrular,
/// iade bildirimlerinde Voided Purchases API ile iade durumunu kontrol eder.
/// Singleton: service account kimliği ve access token önbelleği uygulama ömrü boyunca paylaşılır.
/// </summary>
public sealed class GooglePlayPurchaseVerifier(
    IHttpClientFactory httpClientFactory,
    IOptions<IapOptions> options,
    ILogger<GooglePlayPurchaseVerifier> logger)
{
    private const string AndroidPublisherScope = "https://www.googleapis.com/auth/androidpublisher";
    private readonly IapOptions _options = options.Value;
    private readonly Lazy<GoogleCredential?> _credential = new(() => LoadCredential(options.Value, logger));

    private string ApplicationUrl => "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/"
                                     + Uri.EscapeDataString(_options.GooglePlayPackageName);

    public async Task<IapVerificationResult> VerifyAsync(string productId, string purchaseToken, CancellationToken ct)
    {
        var (accessToken, error) = await GetAccessTokenAsync(ct);
        if (error is not null)
        {
            return error;
        }

        var url = $"{ApplicationUrl}/purchases/products/"
                  + $"{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(purchaseToken)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory.CreateClient().SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Google Play API'ye ulaşılamadı.");
            return IapVerificationResult.Unavailable("Google Play'e ulaşılamadı.");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                logger.LogWarning("Google Play satın almayı tanımadı. Ürün: {ProductId}, HTTP {Status}: {Body}", productId, (int)response.StatusCode, body);
                return IapVerificationResult.Invalid("Satın alma Google Play'de bulunamadı.");
            }

            if (!response.IsSuccessStatusCode)
            {
                // 401/403 genellikle service account'un Play Console'da yetkisi olmadığı anlamına gelir.
                logger.LogError("Google Play API hatası. HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return IapVerificationResult.Unavailable("Google Play doğrulaması şu anda yapılamıyor.");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // purchaseState: 0 = satın alındı, 1 = iptal edildi, 2 = beklemede
            var purchaseState = root.TryGetProperty("purchaseState", out var stateNode) && stateNode.TryGetInt32(out var s) ? s : -1;
            if (purchaseState != 0)
            {
                logger.LogWarning("Google Play satın alması geçerli durumda değil. Ürün: {ProductId}, purchaseState: {State}", productId, purchaseState);
                return IapVerificationResult.Invalid(purchaseState == 2
                    ? "Ödeme henüz tamamlanmadı (beklemede)."
                    : "Satın alma iptal edilmiş.");
            }

            // purchaseType: 0 = lisanslı test hesabı satın alması (ücretsiz), 1 = promosyon kodu, 2 = ödüllü; alan yoksa gerçek ödeme.
            int? purchaseType = root.TryGetProperty("purchaseType", out var typeNode) && typeNode.TryGetInt32(out var pt) ? pt : null;
            if (purchaseType == 0)
            {
                logger.LogInformation("Google Play test satın alması doğrulandı. Ürün: {ProductId}", productId);
            }

            var metadata = new Dictionary<string, object?>
            {
                ["platform"] = "android",
                ["productId"] = productId,
                ["environment"] = purchaseType switch
                {
                    null => "Production",
                    0 => "Test",
                    1 => "Promo",
                    2 => "Rewarded",
                    _ => $"Type{purchaseType}",
                },
            };

            if (root.TryGetProperty("regionCode", out var region) && region.ValueKind == JsonValueKind.String)
            {
                metadata["regionCode"] = region.GetString();
            }

            if (root.TryGetProperty("orderId", out var orderNode) && orderNode.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(orderNode.GetString()))
            {
                var orderId = orderNode.GetString()!;
                metadata["orderId"] = orderId;
                await AddOrderPriceAsync(metadata, orderId, accessToken, ct);
            }

            return IapVerificationResult.Valid(purchaseToken, metadata);
        }
    }

    /// <summary>
    /// İade bildirimi geldiğinde Voided Purchases API (purchases.voidedpurchases.list) ile satın almanın
    /// iade/iptal edilip edilmediğini kontrol eder. API varsayılan olarak son 30 günü döner; bildirim iade
    /// anında geldiği için bu pencere yeterlidir. Service account'un "finansal verileri görüntüleme" izni gerekir.
    /// </summary>
    public async Task<IapRefundCheck> CheckRefundAsync(string purchaseToken, CancellationToken ct)
    {
        var (accessToken, error) = await GetAccessTokenAsync(ct);
        if (error is not null)
        {
            return IapRefundCheck.Of(error.Status == IapVerificationStatus.NotConfigured
                ? IapRefundStatus.NotConfigured
                : IapRefundStatus.StoreUnavailable);
        }

        string? pageToken = null;
        for (var page = 0; page < 10; page++)
        {
            var url = $"{ApplicationUrl}/purchases/voidedpurchases?maxResults=1000"
                      + (pageToken is null ? "" : $"&token={Uri.EscapeDataString(pageToken)}");

            string body;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new("Bearer", accessToken);
                using var response = await httpClientFactory.CreateClient().SendAsync(request, ct);
                body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Google Play Voided Purchases API hatası. HTTP {Status}: {Body}", (int)response.StatusCode, body);
                    return IapRefundCheck.Of(IapRefundStatus.StoreUnavailable);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Google Play Voided Purchases API'ye ulaşılamadı.");
                return IapRefundCheck.Of(IapRefundStatus.StoreUnavailable);
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("voidedPurchases", out var voided) && voided.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in voided.EnumerateArray())
                {
                    if (!item.TryGetProperty("purchaseToken", out var tokenNode) || tokenNode.GetString() != purchaseToken)
                    {
                        continue;
                    }

                    // voidedSource: 0 kullanıcı, 1 geliştirici, 2 Google
                    // voidedReason: 0 diğer, 1 vazgeçme, 2 ulaşmadı, 3 kusurlu, 4 yanlışlıkla, 5 sahtecilik, 6 dostane sahtecilik, 7 ters ibraz
                    // 8 onaylanmamış satın alma: 3 gün içinde tamamlanmayan (tüketilmeyen) satın almayı Google otomatik iade eder;
                    // istemci tokenleri alıp satın almayı tamamlamazsa bu yolla da token düşülür.
                    var metadata = new Dictionary<string, object?>();
                    if (TryReadInt64(item, "voidedTimeMillis", out var voidedMs))
                    {
                        metadata["refundedAt"] = DateTimeOffset.FromUnixTimeMilliseconds(voidedMs);
                    }
                    if (TryReadInt64(item, "voidedSource", out var source))
                    {
                        metadata["voidedSource"] = source;
                    }
                    if (TryReadInt64(item, "voidedReason", out var reason))
                    {
                        metadata["voidedReason"] = reason;
                    }
                    if (item.TryGetProperty("orderId", out var orderNode) && orderNode.ValueKind == JsonValueKind.String)
                    {
                        metadata["orderId"] = orderNode.GetString();
                    }
                    return IapRefundCheck.Refunded(metadata);
                }
            }

            pageToken = root.TryGetProperty("tokenPagination", out var pagination)
                        && pagination.TryGetProperty("nextPageToken", out var next)
                ? next.GetString()
                : null;
            if (string.IsNullOrEmpty(pageToken))
            {
                break;
            }
        }

        return IapRefundCheck.NotRefunded();
    }

    private async Task<(string AccessToken, IapVerificationResult? Error)> GetAccessTokenAsync(CancellationToken ct)
    {
        var credential = _credential.Value;
        if (credential is null || string.IsNullOrWhiteSpace(_options.GooglePlayPackageName))
        {
            return ("", IapVerificationResult.NotConfigured("Google Play doğrulaması yapılandırılmamış (Iap:GoogleServiceAccountPath)."));
        }

        try
        {
            return (await ((ITokenAccess)credential).GetAccessTokenForRequestAsync(cancellationToken: ct), null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Play access token alınamadı.");
            return ("", IapVerificationResult.Unavailable("Google kimlik doğrulaması başarısız."));
        }
    }

    /// <summary>int64 alanları Google API JSON'unda metin olarak gelir; sayı olarak gelenler de kabul edilir.</summary>
    private static bool TryReadInt64(JsonElement parent, string property, out long value)
    {
        value = 0;
        return parent.TryGetProperty(property, out var node)
               && (node.ValueKind == JsonValueKind.String ? long.TryParse(node.GetString(), out value)
                   : node.ValueKind == JsonValueKind.Number && node.TryGetInt64(out value));
    }

    /// <summary>
    /// Admin gelir istatistiği için Orders API'den (GET orders/{orderId}) sipariş tutarını okur.
    /// En iyi çaba: fiyat yalnızca istatistik içindir, bu yüzden hata olursa satın alma yine kredilendirilir,
    /// sadece fiyat alanları boş kalır. Service account'un Play Console'da "finansal verileri görüntüleme"
    /// izni olmalıdır (purchases.products.get için de aynı izin gerekir).
    /// </summary>
    private async Task AddOrderPriceAsync(Dictionary<string, object?> metadata, string orderId, string accessToken, CancellationToken ct)
    {
        var url = $"{ApplicationUrl}/orders/{Uri.EscapeDataString(orderId)}";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new("Bearer", accessToken);
            using var response = await httpClientFactory.CreateClient().SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Play sipariş fiyatı alınamadı. Order: {OrderId}, HTTP {Status}: {Body}", orderId, (int)response.StatusCode, body);
                return;
            }

            using var doc = JsonDocument.Parse(body);
            var order = doc.RootElement;
            if (!TryReadMoneyMilli(order, "total", out var currency, out var totalMilli))
            {
                logger.LogWarning("Google Play sipariş yanıtında toplam tutar yok. Order: {OrderId}", orderId);
                return;
            }

            metadata["currency"] = currency;
            metadata["priceMilli"] = totalMilli;
            if (TryReadMoneyMilli(order, "tax", out _, out var taxMilli))
            {
                metadata["taxMilli"] = taxMilli;
            }
            if (TryReadMoneyMilli(order, "developerRevenueInBuyerCurrency", out _, out var proceedsMilli))
            {
                metadata["proceedsMilli"] = proceedsMilli;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Google Play sipariş fiyatı alınamadı. Order: {OrderId}", orderId);
        }
    }

    /// <summary>Google Money nesnesini ({currencyCode, units, nanos}) 1000 katı tam sayıya çevirir (App Store "price" ile aynı birim).</summary>
    private static bool TryReadMoneyMilli(JsonElement parent, string property, out string? currency, out long milli)
    {
        currency = null;
        milli = 0;
        if (!parent.TryGetProperty(property, out var money) || money.ValueKind != JsonValueKind.Object
            || !money.TryGetProperty("currencyCode", out var code) || code.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        // units int64 olduğu için JSON'da metin olarak gelir; nanos sayıdır. İkisi de yoksa tutar sıfırdır.
        long units = 0;
        if (money.TryGetProperty("units", out var unitsNode)
            && !(unitsNode.ValueKind == JsonValueKind.String ? long.TryParse(unitsNode.GetString(), out units)
                : unitsNode.ValueKind == JsonValueKind.Number && unitsNode.TryGetInt64(out units)))
        {
            return false;
        }

        var nanos = money.TryGetProperty("nanos", out var nanosNode) && nanosNode.ValueKind == JsonValueKind.Number
            && nanosNode.TryGetInt32(out var n) ? n : 0;

        currency = code.GetString();
        milli = units * 1000 + nanos / 1_000_000;
        return true;
    }

    private static GoogleCredential? LoadCredential(IapOptions options, ILogger logger)
    {
        var path = options.GoogleServiceAccountPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            logger.LogError("Google Play service account dosyası bulunamadı: {Path}", path);
            return null;
        }

        try
        {
            return GoogleCredential.FromFile(path).CreateScoped(AndroidPublisherScope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Play service account dosyası okunamadı: {Path}", path);
            return null;
        }
    }
}
