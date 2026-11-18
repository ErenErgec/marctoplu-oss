using System.Text.Json;

namespace MarcToken.Api.Data;

public sealed class Wallet
{
    public Guid UserId { get; set; }
    public int Balance { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TokenPackage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TokenAmount { get; set; }
    public decimal PriceTry { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PackageId { get; set; }
    public string Provider { get; set; } = "iyzico";
    public string ProviderConversationId { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string Status { get; set; } = "pending";
    public decimal AmountTry { get; set; }
    public decimal? PaidAmountTry { get; set; }
    public int TokenAmount { get; set; }
    public string? RawPayload { get; set; }
    public string? RawVerification { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Job
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "queued";
    public int TotalIsbns { get; set; }
    public int ProcessedIsbns { get; set; }
    public int EstimatedTokens { get; set; }
    public int ConsumedTokens { get; set; }
    public int RefundedTokens { get; set; }
    public string InputFilePath { get; set; } = string.Empty;
    public string? OutputFilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TokenLedger
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? JobId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");
}

public sealed class UserProfile
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? IdentityNumber { get; set; }
    public string? GsmNumber { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public string? FcmToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class DeviceTrial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public Guid FirstUserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset ClaimedAt { get; set; } = DateTimeOffset.UtcNow;
}


public sealed class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid? JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
