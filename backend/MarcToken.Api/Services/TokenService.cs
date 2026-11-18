using System.Text.Json;
using Microsoft.Extensions.Options;
using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MarcToken.Api.Services;

public sealed class TokenService(
    AppDbContext db,
    IOptions<AppOptions> options,
    ILogger<TokenService> logger)
{
    private readonly AppOptions _options = options.Value;

    public async Task<int> GetBalanceAsync(
        Guid userId,
        string? deviceId = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        await EnsureWalletExistsAsync(userId, deviceId, ipAddress, ct);
        return await db.Wallets
            .Where(x => x.UserId == userId)
            .Select(x => x.Balance)
            .FirstAsync(ct);
    }

    /// <summary>
    /// Cüzdanı oluşturmadan bakiyeyi okur (cüzdan yoksa 0). Cüzdan oluşturma ücretsiz deneme hakkını
    /// cihaz kimliğiyle verdiği için ön kontrollerde GetBalanceAsync yerine bu kullanılmalı.
    /// </summary>
    public async Task<int> PeekBalanceAsync(Guid userId, CancellationToken ct = default)
        => await db.Wallets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Balance)
            .FirstOrDefaultAsync(ct) ?? 0;

    public async Task ConsumeAsync(Guid userId, Guid jobId, int amount, string reason, CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Consume amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Consume reason is required.");
        }

        var now = DateTimeOffset.UtcNow;
        await EnsureWalletExistsAsync(userId, ct: ct, now: now);

        await ExecuteInTransactionAsync(async () =>
        {
            var updatedRows = await db.Database.ExecuteSqlInterpolatedAsync($@"
                update wallets
                set balance = balance - {amount}, updated_at = {now}
                where user_id = {userId} and balance >= {amount}", ct);

            if (updatedRows == 0)
            {
                throw new InvalidOperationException("Yetersiz token bakiyesi. Lütfen token satın alın.");
            }

            db.TokenLedgers.Add(new TokenLedger
            {
                UserId = userId,
                JobId = jobId,
                Delta = -amount,
                Reason = reason.Trim(),
            });

            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task CreditAsync(Guid userId, Guid? paymentId, Guid? jobId, int amount, string reason, CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Credit reason is required.");
        }

        var now = DateTimeOffset.UtcNow;
        await EnsureWalletExistsAsync(userId, ct: ct, now: now);

        await ExecuteInTransactionAsync(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                update wallets
                set balance = balance + {amount}, updated_at = {now}
                where user_id = {userId}", ct);

            db.TokenLedgers.Add(new TokenLedger
            {
                UserId = userId,
                PaymentId = paymentId,
                JobId = jobId,
                Delta = amount,
                Reason = reason.Trim(),
            });

            await db.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>
    /// Aynı <paramref name="reason"/> ile daha önce kredi yüklenmediyse yükler; yüklendiyse hiçbir şey yapmaz.
    /// Aynı reason için eşzamanlı istekler transaction ömürlü advisory lock ile sıraya sokulur,
    /// böylece iki paralel istek aynı satın almayı iki kez kredilendiremez.
    /// </summary>
    /// <param name="metadata">Ledger satırının metadata (jsonb) alanına yazılır; örn. IAP fiyatı ve para birimi.</param>
    /// <returns>Kredi bu çağrıda yüklendiyse true; daha önce yüklenmişse false.</returns>
    public Task<bool> CreditOnceAsync(
        Guid userId,
        int amount,
        string reason,
        IReadOnlyDictionary<string, object?>? metadata = null,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Credit amount must be positive.");
        }

        return ApplyOnceAsync(userId, amount, reason, metadata, ct);
    }

    /// <summary>
    /// Aynı <paramref name="reason"/> ile daha önce düşülmediyse bakiyeden düşer (mağaza iadesi).
    /// <see cref="ConsumeAsync"/>'ten farklı olarak bakiye yetmese de tutarın tamamı düşülür ve bakiye eksiye
    /// inebilir: iade edilen tokenler harcanmışsa kullanıcı yeni satın alma yapana kadar iş başlatamaz.
    /// </summary>
    /// <returns>Tutar bu çağrıda düşüldüyse true; daha önce düşülmüşse false.</returns>
    public Task<bool> DebitOnceAsync(
        Guid userId,
        int amount,
        string reason,
        IReadOnlyDictionary<string, object?>? metadata = null,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Debit amount must be positive.");
        }

        return ApplyOnceAsync(userId, -amount, reason, metadata, ct);
    }

    private async Task<bool> ApplyOnceAsync(
        Guid userId,
        int delta,
        string reason,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Ledger reason is required.");
        }

        var trimmedReason = reason.Trim();
        var now = DateTimeOffset.UtcNow;
        await EnsureWalletExistsAsync(userId, ct: ct, now: now);

        var applied = false;
        await ExecuteInTransactionAsync(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"select pg_advisory_xact_lock(hashtext({trimmedReason}))", ct);

            if (await db.TokenLedgers.AnyAsync(x => x.Reason == trimmedReason, ct))
            {
                return;
            }

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                update wallets
                set balance = balance + {delta}, updated_at = {now}
                where user_id = {userId}", ct);

            db.TokenLedgers.Add(new TokenLedger
            {
                UserId = userId,
                Delta = delta,
                Reason = trimmedReason,
                MetadataJson = JsonSerializer.SerializeToDocument(metadata ?? new Dictionary<string, object?>()),
            });

            await db.SaveChangesAsync(ct);
            applied = true;
        }, ct);

        return applied;
    }

    private async Task EnsureWalletExistsAsync(
        Guid userId,
        string? deviceId = null,
        string? ipAddress = null,
        CancellationToken ct = default,
        DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;

        var walletExists = await db.Wallets.AnyAsync(x => x.UserId == userId, ct);
        if (walletExists)
        {
            return;
        }

        var initialBalance = 0;

        // Cihaz bazlı suistimal korumalı deneme hakkı:
        // 1. Cihaz kimliği (X-Device-Id) mobil uygulama tarafından gönderilmiş olmalı (web_ öneki hariç).
        // 2. Bu cihaz daha önce hiç ücretsiz hak almamış olmalı.
        // 3. Aynı IP adresinden son 24 saatteki deneme hakkı talebi sınırı (MaxTrialsPerIpPerDay) aşılmamış olmalı.
        if (!string.IsNullOrWhiteSpace(deviceId) &&
            deviceId.Trim().Length >= 8 &&
            !deviceId.Trim().StartsWith("web_", StringComparison.OrdinalIgnoreCase))
        {
            var cleanDeviceId = deviceId.Trim();
            var alreadyClaimed = await db.DeviceTrials.AnyAsync(x => x.DeviceId == cleanDeviceId, ct);

            if (!alreadyClaimed)
            {
                var ipCount = 0;
                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    var cutoff = timestamp.AddHours(-24);
                    ipCount = await db.DeviceTrials.CountAsync(x => x.IpAddress == ipAddress && x.ClaimedAt >= cutoff, ct);
                }

                if (ipCount < _options.MaxTrialsPerIpPerDay)
                {
                    initialBalance = _options.FreeTrialTokens;

                    db.DeviceTrials.Add(new DeviceTrial
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = cleanDeviceId,
                        FirstUserId = userId,
                        IpAddress = ipAddress,
                        ClaimedAt = timestamp,
                    });

                    if (initialBalance > 0)
                    {
                        db.TokenLedgers.Add(new TokenLedger
                        {
                            UserId = userId,
                            Delta = initialBalance,
                            Reason = "welcome_device_trial",
                            CreatedAt = timestamp,
                        });
                    }

                    logger.LogInformation("New device trial granted to DeviceId: {DeviceId}, UserId: {UserId}, Tokens: {Tokens}", cleanDeviceId, userId, initialBalance);
                }
                else
                {
                    logger.LogWarning("Device trial skipped for IP {IpAddress} due to rate limit.", ipAddress);
                }
            }
            else
            {
                logger.LogInformation("Device {DeviceId} already claimed free trial tokens previously.", cleanDeviceId);
            }
        }

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            insert into wallets (user_id, balance, created_at, updated_at)
            values ({userId}, {initialBalance}, {timestamp}, {timestamp})
            on conflict (user_id) do nothing", ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await operation();
            return;
        }

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        await operation();
        await tx.CommitAsync(ct);
    }
}
