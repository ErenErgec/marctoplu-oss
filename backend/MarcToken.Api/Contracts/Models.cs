using System.ComponentModel.DataAnnotations;

namespace MarcToken.Api.Contracts;

public sealed record WalletResponse(int Balance);

public sealed record UserProfileResponse(
    string? Email,
    string? Name,
    string? Surname,
    string? IdentityNumber,
    string? GsmNumber,
    string? City,
    string? Country,
    string? Address,
    string? ZipCode,
    DateTimeOffset? UpdatedAt
);

public sealed class UpdateUserProfileRequest
{
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [MinLength(2, ErrorMessage = "Ad en az 2 karakter olmalıdır.")]
    [MaxLength(50)]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [MinLength(2, ErrorMessage = "Soyad en az 2 karakter olmalıdır.")]
    [MaxLength(50)]
    public string? Surname { get; init; }

    [RegularExpression("^[0-9]{11}$")]
    public string? IdentityNumber { get; init; }

    [RegularExpression("^\\+?[0-9]{10,15}$")]
    public string? GsmNumber { get; init; }

    [MaxLength(60)]
    public string? City { get; init; }

    [MaxLength(60)]
    public string? Country { get; init; }

    [MaxLength(200)]
    public string? Address { get; init; }

    [MaxLength(10)]
    public string? ZipCode { get; init; }
}

public sealed record TokenPackageResponse(Guid Id, string Name, int TokenAmount, decimal PriceTry);

public sealed class CheckoutBuyerInfo
{
    [Required, MinLength(2), MaxLength(50)]
    public string Name { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(50)]
    public string Surname { get; init; } = string.Empty;

    [Required, RegularExpression("^[0-9]{11}$")]
    public string IdentityNumber { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(60)]
    public string City { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(60)]
    public string Country { get; init; } = string.Empty;

    [Required, MinLength(5), MaxLength(200)]
    public string Address { get; init; } = string.Empty;

    [Required, MinLength(4), MaxLength(10)]
    public string ZipCode { get; init; } = string.Empty;

    [Required, RegularExpression("^\\+?[0-9]{10,15}$")]
    public string GsmNumber { get; init; } = string.Empty;
}

public sealed class CheckoutInitRequest
{
    [Required]
    public Guid PackageId { get; init; }

    [Required]
    public CheckoutBuyerInfo Buyer { get; init; } = new();
}

public sealed class VerifyIapReceiptRequest
{
    [Required]
    public string ProductId { get; init; } = string.Empty;

    [Required]
    public string PurchaseToken { get; init; } = string.Empty;

    public string? ReceiptData { get; init; }

    /// <summary>iOS için zorunlu: App Store transactionId (PurchaseDetails.purchaseID).</summary>
    public string? TransactionId { get; init; }

    [Required]
    public string Platform { get; init; } = "android"; // "ios" or "android"
}

public sealed record UpdateFcmTokenRequest(string FcmToken);

public sealed record CheckoutInitResponse(Guid PaymentId, string ConversationId, string CheckoutUrl);

public sealed record JobCreateResponse(Guid JobId, string Status, int EstimatedTokens);

public sealed record JobResponse(
    Guid Id,
    string Status,
    int TotalIsbns,
    int ProcessedIsbns,
    int ConsumedTokens,
    int RefundedTokens,
    string? ErrorMessage,
    bool CanDownload,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed class CreateJobFromIsbnsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "En az bir ISBN gereklidir.")]
    public List<string> Isbns { get; init; } = new();
}

// ────────────────────────────────────────────────────────────────
// Admin
// ────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/admin/stats yanıtı. IAP metrikleri token_ledger'daki "iap:%" kredilerinden sayılır.
/// Fiyat, doğrulama sırasında mağazadan okunup ledger metadata'sına yazılır (iOS: App Store işlem bilgisi, Android: Orders API).
/// iyzico alanları askıdaki web ödemelerine aittir.
/// </summary>
public sealed record AdminStatsResponse(
    int TotalUsers,
    int TotalJobs,
    int CompletedJobs,
    int FailedJobs,
    int RunningJobs,
    int QueuedJobs,
    long TotalIsbnProcessed,
    long TotalTokensConsumed,
    int IapPurchases,
    long IapTokensSold,
    AdminIapPlatformStats IapAndroid,
    AdminIapPlatformStats IapIos,
    IReadOnlyList<AdminIapRevenue> IapIosRevenue,
    IReadOnlyList<AdminIapRevenue> IapAndroidRevenue,
    int IapRefunds,
    int IyzicoPayments,
    double IyzicoRevenueTry
);

public sealed record AdminIapPlatformStats(int Purchases, long TokensSold);

/// <summary>
/// Fiyatı kayıtlı, iade edilmemiş production satın almalarının para birimi başına toplamı (platform başına).
/// Müşterinin ödediği tutardır: vergiler ve mağaza komisyonu düşülmemiştir.
/// </summary>
public sealed record AdminIapRevenue(string Currency, int Purchases, decimal Amount);

/// <summary>
/// Admin "Satın Almalar" listesi: filtreye uyan son satın almalar ve tüm filtrelenmiş kümenin özeti.
/// Revenue yalnızca fiyatı kayıtlı ve iade edilmemiş satırları içerir (brüt; vergi ve mağaza komisyonu düşülmemiş).
/// </summary>
public sealed record AdminPurchaseListResponse(
    int TotalCount,
    long TotalTokens,
    int RefundedCount,
    IReadOnlyList<AdminIapRevenue> Revenue,
    IReadOnlyList<AdminPurchaseResponse> Items
);

/// <summary>
/// Tek bir IAP kredisi (token_ledger "iap:" satırı). Fiyat alanları, fiyat kaydı özelliğinden önceki
/// satın almalarda ve Orders API'nin yanıt vermediği durumlarda boştur.
/// </summary>
public sealed record AdminPurchaseResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid UserId,
    string? Email,
    string? Name,
    string Platform,
    string? ProductId,
    int Tokens,
    string? Environment,
    string? Currency,
    decimal? Price,
    decimal? Tax,
    decimal? Proceeds,
    string? Region,
    string? OrderId,
    string StoreReference,
    DateTimeOffset? RefundedAt
);

public sealed record AdminPackageResponse(
    Guid Id,
    string Name,
    int TokenAmount,
    decimal PriceTry,
    bool Active,
    DateTimeOffset CreatedAt
);

public sealed class AdminPackageRequest
{
    [Required, MinLength(2), MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 1_000_000)]
    public int TokenAmount { get; init; }

    [Range(0.01, 1_000_000)]
    public decimal PriceTry { get; init; }

    public bool Active { get; init; } = true;
}

public sealed class AdminAddTokensRequest
{
    [Required(ErrorMessage = "Kullanıcı bilgisi (e-posta veya ID) gereklidir.")]
    public string UserIdentifier { get; init; } = string.Empty;

    [Range(1, 1_000_000, ErrorMessage = "Token miktarı 1 ile 1.000.000 arasında olmalıdır.")]
    public int Amount { get; init; }

    [MaxLength(200)]
    public string? Reason { get; init; }
}

public sealed record AdminAddTokensResponse(
    bool Success,
    Guid UserId,
    string? Email,
    int PreviousBalance,
    int AddedAmount,
    int NewBalance,
    string? Reason
);

public sealed record AdminUserSummaryResponse(
    Guid UserId,
    string? Email,
    string? Name,
    int Balance,
    int TotalJobs,
    int ConsumedTokens,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);


public sealed record CreateSupportTicketRequest
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public Guid? JobId { get; init; }

    [Required(ErrorMessage = "Sorun açıklaması zorunludur.")]
    [MinLength(10, ErrorMessage = "Sorun açıklaması en az 10 karakter olmalıdır.")]
    [MaxLength(4000, ErrorMessage = "Sorun açıklaması en fazla 4000 karakter olabilir.")]
    public string Message { get; init; } = string.Empty;
}

public sealed record SupportTicketCreatedResponse(Guid Id, DateTimeOffset CreatedAt);

public sealed record AdminSupportTicketResponse(
    Guid Id,
    Guid UserId,
    string Email,
    Guid? JobId,
    string? JobStatus,
    string Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt
);
