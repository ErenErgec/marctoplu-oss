using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

/// <summary>
/// App Store Server API (GET /inApps/v1/transactions/{transactionId}) ile satın almayı doğrular.
/// Production'da bulunamazsa (ve izin verildiyse) sandbox ortamında da arar.
/// Singleton: .p8 imza anahtarı bir kez yüklenir.
/// </summary>
public sealed class AppStorePurchaseVerifier(
    IHttpClientFactory httpClientFactory,
    IOptions<IapOptions> options,
    ILogger<AppStorePurchaseVerifier> logger)
{
    private const string ProductionBaseUrl = "https://api.storekit.itunes.apple.com";
    private const string SandboxBaseUrl = "https://api.storekit-sandbox.itunes.apple.com";

    private readonly IapOptions _options = options.Value;
    private readonly Lazy<ECDsa?> _signingKey = new(() => LoadSigningKey(options.Value, logger));

    public async Task<IapVerificationResult> VerifyAsync(string productId, string transactionId, CancellationToken ct)
    {
        var (error, payload, environment) = await FetchTransactionAsync(transactionId, ct);
        if (error is not null)
        {
            return error;
        }

        using var transaction = payload!;
        var tx = transaction.RootElement;
        var bundleId = tx.TryGetProperty("bundleId", out var b) ? b.GetString() : null;
        var txProductId = tx.TryGetProperty("productId", out var p) ? p.GetString() : null;
        var appleTransactionId = tx.TryGetProperty("transactionId", out var t) ? t.GetString() : null;
        var revoked = tx.TryGetProperty("revocationDate", out _);

        if (!string.Equals(bundleId, _options.AppleBundleId, StringComparison.Ordinal))
        {
            logger.LogWarning("App Store işlemi başka bir uygulamaya ait. Beklenen {Expected}, gelen {Actual}", _options.AppleBundleId, bundleId);
            return IapVerificationResult.Invalid("Satın alma bu uygulamaya ait değil.");
        }

        if (!string.Equals(txProductId, productId, StringComparison.Ordinal))
        {
            logger.LogWarning("App Store ürün uyuşmazlığı. İstenen {Requested}, işlem {Actual}", productId, txProductId);
            return IapVerificationResult.Invalid("Satın alınan ürün eşleşmiyor.");
        }

        if (revoked)
        {
            return IapVerificationResult.Invalid("Satın alma iade edilmiş veya iptal edilmiş.");
        }

        if (string.IsNullOrWhiteSpace(appleTransactionId))
        {
            return IapVerificationResult.Unavailable("App Store yanıtında işlem kimliği yok.");
        }

        if (environment == "Sandbox")
        {
            logger.LogInformation("App Store sandbox satın alması doğrulandı. Ürün: {ProductId}", productId);
        }

        return IapVerificationResult.Valid(appleTransactionId, ReadPurchaseMetadata(tx, productId, environment));
    }

    /// <summary>
    /// İade bildirimi geldiğinde işlemin güncel durumunu App Store Server API'den okur.
    /// Apple iade edilen işleme revocationDate (ms) ve revocationReason (0 = diğer, 1 = uygulama sorunu) ekler;
    /// iade geri alınırsa bu alanlar kalkar.
    /// </summary>
    public async Task<IapRefundCheck> CheckRefundAsync(string transactionId, CancellationToken ct)
    {
        var (error, payload, environment) = await FetchTransactionAsync(transactionId, ct);
        if (error is not null)
        {
            return IapRefundCheck.Of(error.Status switch
            {
                IapVerificationStatus.NotConfigured => IapRefundStatus.NotConfigured,
                IapVerificationStatus.Invalid => IapRefundStatus.NotFound,
                _ => IapRefundStatus.StoreUnavailable,
            });
        }

        using var transaction = payload!;
        var tx = transaction.RootElement;
        var bundleId = tx.TryGetProperty("bundleId", out var b) ? b.GetString() : null;
        if (!string.Equals(bundleId, _options.AppleBundleId, StringComparison.Ordinal))
        {
            logger.LogWarning("İade kontrolü: App Store işlemi başka bir uygulamaya ait. Gelen {Actual}", bundleId);
            return IapRefundCheck.Of(IapRefundStatus.NotFound);
        }

        if (!tx.TryGetProperty("revocationDate", out var revocationNode) || !revocationNode.TryGetInt64(out var revocationMs))
        {
            return IapRefundCheck.NotRefunded();
        }

        var metadata = new Dictionary<string, object?>
        {
            ["environment"] = environment,
            ["refundedAt"] = DateTimeOffset.FromUnixTimeMilliseconds(revocationMs),
        };
        if (tx.TryGetProperty("revocationReason", out var reasonNode) && reasonNode.TryGetInt32(out var reason))
        {
            metadata["revocationReason"] = reason;
        }

        return IapRefundCheck.Refunded(metadata);
    }

    /// <summary>
    /// İşlemi App Store Server API'den çeker ve signedTransactionInfo payload'unu döndürür.
    /// Başarısızsa Error doludur (NotConfigured, Invalid = işlem bulunamadı, StoreUnavailable).
    /// </summary>
    private async Task<(IapVerificationResult? Error, JsonDocument? Payload, string Environment)> FetchTransactionAsync(
        string transactionId, CancellationToken ct)
    {
        var key = _signingKey.Value;
        if (key is null
            || string.IsNullOrWhiteSpace(_options.AppleIssuerId)
            || string.IsNullOrWhiteSpace(_options.AppleKeyId)
            || string.IsNullOrWhiteSpace(_options.AppleBundleId))
        {
            return (IapVerificationResult.NotConfigured(
                "App Store doğrulaması yapılandırılmamış (Iap:AppleIssuerId, Iap:AppleKeyId, Iap:ApplePrivateKeyPath)."), null, "");
        }

        var jwt = CreateAuthToken(key);

        var (status, body) = await GetTransactionAsync(ProductionBaseUrl, transactionId, jwt, ct);
        var environment = "Production";
        // 404: işlem production'da yok (sandbox satın alması olabilir).
        // 401: Apple, App Store'da henüz yayınlanmamış uygulamalar için production'da 401 döner;
        // App Review satın almaları sandbox'ta olduğundan bu durumda da sandbox denenir.
        // Kimlik bilgileri gerçekten hatalıysa sandbox da 401 döner ve sonuç değişmez.
        if (status is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized && _options.AppleAllowSandbox)
        {
            (status, body) = await GetTransactionAsync(SandboxBaseUrl, transactionId, jwt, ct);
            environment = "Sandbox";
        }

        if (status is null)
        {
            return (IapVerificationResult.Unavailable("App Store'a ulaşılamadı."), null, environment);
        }

        if (status is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            logger.LogWarning("App Store işlemi tanımadı. Transaction: {TransactionId}, HTTP {Status}: {Body}", transactionId, (int)status, body);
            return (IapVerificationResult.Invalid("Satın alma App Store'da bulunamadı."), null, environment);
        }

        if (status != HttpStatusCode.OK)
        {
            // 401 genellikle Issuer ID / Key ID / .p8 anahtarının hatalı olduğu anlamına gelir.
            logger.LogError("App Store Server API hatası. HTTP {Status}: {Body}", (int)status, body);
            return (IapVerificationResult.Unavailable("App Store doğrulaması şu anda yapılamıyor."), null, environment);
        }

        // signedTransactionInfo bir JWS'tir. Yanıtı kimliği doğrulanmış TLS isteğiyle doğrudan Apple'dan
        // aldığımız için imza zinciri ayrıca doğrulanmıyor; yalnızca payload okunuyor.
        using var doc = JsonDocument.Parse(body!);
        var signed = doc.RootElement.TryGetProperty("signedTransactionInfo", out var signedNode) ? signedNode.GetString() : null;
        var payload = JwsPayload.TryParse(signed);
        if (payload is null)
        {
            logger.LogError("App Store yanıtında signedTransactionInfo yok veya bozuk: {Body}", body);
            return (IapVerificationResult.Unavailable("App Store yanıtı okunamadı."), null, environment);
        }

        return (null, payload, environment);
    }

    /// <summary>
    /// Admin gelir istatistiği için satın alma ayrıntıları. Apple "price" alanını müşterinin ödediği fiyatın
    /// 1000 katı olarak, "currency" alanını ISO 4217 koduyla verir (vergiler ve Apple komisyonu düşülmemiş).
    /// Alanlar yoksa (eski işlemler) yalnızca mevcut olanlar yazılır.
    /// </summary>
    private static Dictionary<string, object?> ReadPurchaseMetadata(JsonElement tx, string productId, string environment)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["platform"] = "ios",
            ["productId"] = productId,
            ["environment"] = environment,
        };

        if (tx.TryGetProperty("storefront", out var storefront) && storefront.ValueKind == JsonValueKind.String)
        {
            metadata["storefront"] = storefront.GetString();
        }

        if (tx.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Number && price.TryGetInt64(out var priceMilli)
            && tx.TryGetProperty("currency", out var currency) && currency.ValueKind == JsonValueKind.String)
        {
            metadata["currency"] = currency.GetString();
            metadata["priceMilli"] = priceMilli;
        }

        return metadata;
    }

    private async Task<(HttpStatusCode? Status, string? Body)> GetTransactionAsync(
        string baseUrl, string transactionId, string jwt, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/inApps/v1/transactions/{Uri.EscapeDataString(transactionId)}");
        request.Headers.Authorization = new("Bearer", jwt);
        try
        {
            using var response = await httpClientFactory.CreateClient().SendAsync(request, ct);
            return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "App Store Server API'ye ulaşılamadı: {BaseUrl}", baseUrl);
            return (null, null);
        }
    }

    /// <summary>App Store Server API için ES256 imzalı kısa ömürlü JWT.</summary>
    private string CreateAuthToken(ECDsa key)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = JsonSerializer.Serialize(new { alg = "ES256", kid = _options.AppleKeyId, typ = "JWT" });
        var claims = JsonSerializer.Serialize(new
        {
            iss = _options.AppleIssuerId,
            iat = now,
            exp = now + 300,
            aud = "appstoreconnect-v1",
            bid = _options.AppleBundleId,
        });

        var signingInput = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(header))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(claims))}";
        // .NET ECDsa.SignData varsayılan olarak JWS'in beklediği IEEE P1363 (r||s) formatını üretir.
        var signature = key.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256);
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static ECDsa? LoadSigningKey(IapOptions options, ILogger logger)
    {
        var path = options.ApplePrivateKeyPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            logger.LogError("App Store In-App Purchase anahtarı (.p8) bulunamadı: {Path}", path);
            return null;
        }

        try
        {
            var key = ECDsa.Create();
            key.ImportFromPem(File.ReadAllText(path));
            return key;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "App Store In-App Purchase anahtarı (.p8) okunamadı: {Path}", path);
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
