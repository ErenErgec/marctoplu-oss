namespace MarcToken.Api.Services;

public enum IapVerificationStatus
{
    /// <summary>Mağaza satın almayı onayladı.</summary>
    Valid,
    /// <summary>Mağaza satın almayı tanımıyor, iptal edilmiş/iade edilmiş veya ürün eşleşmiyor.</summary>
    Invalid,
    /// <summary>Platform için doğrulama anahtarları tanımlı değil.</summary>
    NotConfigured,
    /// <summary>Mağazaya ulaşılamadı veya yetki hatası; istemci daha sonra tekrar denemeli.</summary>
    StoreUnavailable,
}

/// <param name="TransactionKey">Mağazanın tekil işlem kimliği; ledger'da tekrar kullanım koruması için anahtar.</param>
/// <param name="Metadata">Mağazanın bildirdiği satın alma ayrıntıları (fiyat, para birimi…); ledger'ın metadata alanına yazılır.</param>
public sealed record IapVerificationResult(
    IapVerificationStatus Status,
    string? TransactionKey = null,
    string? Error = null,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static IapVerificationResult Valid(string transactionKey, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(IapVerificationStatus.Valid, transactionKey, Metadata: metadata);
    public static IapVerificationResult Invalid(string error) => new(IapVerificationStatus.Invalid, Error: error);
    public static IapVerificationResult NotConfigured(string error) => new(IapVerificationStatus.NotConfigured, Error: error);
    public static IapVerificationResult Unavailable(string error) => new(IapVerificationStatus.StoreUnavailable, Error: error);
}

public enum IapRefundStatus
{
    /// <summary>Mağaza satın almanın iade edildiğini veya iptal edildiğini bildiriyor.</summary>
    Refunded,
    /// <summary>Satın alma mağazada geçerli (iade yok veya iade geri alınmış).</summary>
    NotRefunded,
    /// <summary>Mağaza işlemi tanımıyor veya işlem başka bir uygulamaya ait.</summary>
    NotFound,
    /// <summary>Platform için doğrulama anahtarları tanımlı değil.</summary>
    NotConfigured,
    /// <summary>Mağazaya ulaşılamadı; bildirim daha sonra tekrar işlenmeli.</summary>
    StoreUnavailable,
}

/// <summary>Mağazanın kimliği doğrulanmış API'sinden okunan iade durumu.</summary>
/// <param name="Metadata">İade ayrıntıları (tarih, sebep…); iade ledger satırının metadata alanına yazılır.</param>
public sealed record IapRefundCheck(
    IapRefundStatus Status,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static IapRefundCheck Refunded(IReadOnlyDictionary<string, object?> metadata) => new(IapRefundStatus.Refunded, metadata);
    public static IapRefundCheck NotRefunded() => new(IapRefundStatus.NotRefunded);
    public static IapRefundCheck Of(IapRefundStatus status) => new(status);
}
