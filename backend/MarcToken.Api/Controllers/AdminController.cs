using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarcToken.Api.Contracts;
using MarcToken.Api.Data;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Controllers;

/// <summary>
/// Admin paneli için yönetim endpoint'leri.
/// Erişim sadece App:AdminEmails listesindeki e-posta adreslerine açıktır.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminController(
    AppDbContext db,
    IOptions<AppOptions> options,
    ILogger<AdminController> logger) : ControllerBase
{
    private readonly AppOptions _options = options.Value;

    // ────────────────────────────────────────────────────────
    // Yardımcılar
    // ────────────────────────────────────────────────────────

    private IActionResult? EnsureAdmin()
    {
        var email = UserContext.GetEmail(User)?.ToLowerInvariant();
        var adminList = _options.GetAdminEmailList();

        if (adminList.Count == 0)
        {
            return StatusCode(503, new { message = "Admin paneli devre dışı. App:AdminEmails ayarını yapılandırın." });
        }

        if (string.IsNullOrWhiteSpace(email) || !adminList.Contains(email))
        {
            logger.LogWarning("Yetkisiz admin erişim girişimi. Email={Email}", email);
            return Forbid();
        }

        return null;
    }

    [HttpGet("check")]
    public IActionResult Check()
    {
        if (EnsureAdmin() is { } err) return err;
        return Ok(new { isAdmin = true, email = UserContext.GetEmail(User) });
    }

    // ────────────────────────────────────────────────────────
    // Genel İstatistikler
    // ────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        var totalUsers = await db.Wallets.CountAsync(ct);
        var totalJobs = await db.Jobs.CountAsync(ct);
        var completedJobs = await db.Jobs.CountAsync(x => x.Status == "completed", ct);
        var failedJobs = await db.Jobs.CountAsync(x => x.Status == "failed", ct);
        var runningJobs = await db.Jobs.CountAsync(x => x.Status == "running", ct);
        var queuedJobs = await db.Jobs.CountAsync(x => x.Status == "queued", ct);
        var totalIsbnProcessed = await db.Jobs.SumAsync(x => (long)x.ProcessedIsbns, ct);
        var totalTokensConsumed = await db.Jobs.SumAsync(x => (long)x.ConsumedTokens, ct);

        // Mobil IAP: doğrulanan her satın alma ledger'a "iap:<platform>:<mağaza kimliği>" reason'ıyla bir kez yazılır.
        var iapTotal = await GetIapStatsAsync("iap:", ct);
        var iapAndroid = await GetIapStatsAsync("iap:android:", ct);
        var iapIos = await GetIapStatsAsync("iap:ios:", ct);
        var iapIosRevenue = await GetIapRevenueAsync("iap:ios:", ct);
        var iapAndroidRevenue = await GetIapRevenueAsync("iap:android:", ct);
        // İade satırı sayısından geri alınan iadeler düşülür ("iap_refund_reversed:" "iap_refund:" ile başlamaz).
        var iapRefunds = await db.TokenLedgers.CountAsync(x => x.Reason.StartsWith(IapRefundService.RefundReasonPrefix), ct)
                         - await db.TokenLedgers.CountAsync(x => x.Reason.StartsWith(IapRefundService.RefundReversedReasonPrefix), ct);

        // iyzico (askıda): yalnızca web checkout ile ödenmiş Payment kayıtları.
        var iyzicoRevenue = await db.Payments
            .Where(x => x.Status == "paid")
            .SumAsync(x => (double)x.PaidAmountTry.GetValueOrDefault(), ct);
        var iyzicoPayments = await db.Payments.CountAsync(x => x.Status == "paid", ct);

        return Ok(new AdminStatsResponse(
            TotalUsers: totalUsers,
            TotalJobs: totalJobs,
            CompletedJobs: completedJobs,
            FailedJobs: failedJobs,
            RunningJobs: runningJobs,
            QueuedJobs: queuedJobs,
            TotalIsbnProcessed: totalIsbnProcessed,
            TotalTokensConsumed: totalTokensConsumed,
            IapPurchases: iapTotal.Purchases,
            IapTokensSold: iapTotal.TokensSold,
            IapAndroid: iapAndroid,
            IapIos: iapIos,
            IapIosRevenue: iapIosRevenue,
            IapAndroidRevenue: iapAndroidRevenue,
            IapRefunds: iapRefunds,
            IyzicoPayments: iyzicoPayments,
            IyzicoRevenueTry: Math.Round(iyzicoRevenue, 2)));
    }

    /// <summary>
    /// Mağaza doğrulamasında ledger metadata'sına yazılan fiyatları para birimine göre toplar
    /// (iOS: App Store işlem bilgisi, Android: Orders API). Yalnızca environment = Production sayılır:
    /// sandbox/TestFlight, lisanslı test ve promosyon satın almaları gerçek ödeme değildir.
    /// Fiyat kaydı eklenmeden önceki satın almaların metadata'sında currency olmadığı için onlar da dışarıda kalır.
    /// İade edilen satın almalar sayılmaz.
    /// </summary>
    private Task<List<AdminIapRevenue>> GetIapRevenueAsync(string reasonPrefix, CancellationToken ct)
        => SumRevenueAsync(
            WhereRefunded(
                db.TokenLedgers
                    .AsNoTracking()
                    .Where(x => x.Delta > 0
                        && x.Reason.StartsWith(reasonPrefix)
                        && x.MetadataJson.RootElement.GetProperty("environment").GetString() == "Production"),
                refunded: false),
            ct);

    /// <summary>
    /// IAP kredi satırlarını iade durumuna göre süzer. "iap:&lt;platform&gt;:&lt;ref&gt;" satırının iadesi
    /// "iap_refund:&lt;platform&gt;:&lt;ref&gt;"; "iap_refund_reversed:" satırı varsa iade geri alınmıştır.
    /// </summary>
    private IQueryable<TokenLedger> WhereRefunded(IQueryable<TokenLedger> credits, bool refunded) => refunded
        ? credits.Where(x => db.TokenLedgers.Any(r => r.Reason == IapRefundService.RefundReasonPrefix + x.Reason.Substring(4))
            && !db.TokenLedgers.Any(r => r.Reason == IapRefundService.RefundReversedReasonPrefix + x.Reason.Substring(4)))
        : credits.Where(x => !db.TokenLedgers.Any(r => r.Reason == IapRefundService.RefundReasonPrefix + x.Reason.Substring(4))
            || db.TokenLedgers.Any(r => r.Reason == IapRefundService.RefundReversedReasonPrefix + x.Reason.Substring(4)));

    /// <summary>Verilen ledger kümesinde fiyatı kayıtlı satırları para birimine göre toplar (TRY önce).</summary>
    private static async Task<List<AdminIapRevenue>> SumRevenueAsync(IQueryable<TokenLedger> credits, CancellationToken ct)
    {
        var rows = await credits
            .Where(x => x.MetadataJson.RootElement.GetProperty("currency").GetString() != null)
            .GroupBy(x => x.MetadataJson.RootElement.GetProperty("currency").GetString()!)
            .Select(g => new
            {
                Currency = g.Key,
                Purchases = g.Count(),
                PriceMilli = g.Sum(x => x.MetadataJson.RootElement.GetProperty("priceMilli").GetInt64()),
            })
            .ToListAsync(ct);

        return rows
            .Select(x => new AdminIapRevenue(x.Currency, x.Purchases, x.PriceMilli / 1000m))
            .OrderByDescending(x => x.Currency == "TRY")
            .ThenByDescending(x => x.Purchases)
            .ToList();
    }

    private async Task<AdminIapPlatformStats> GetIapStatsAsync(string reasonPrefix, CancellationToken ct)
    {
        var credits = db.TokenLedgers
            .AsNoTracking()
            .Where(x => x.Delta > 0 && x.Reason.StartsWith(reasonPrefix));

        return new AdminIapPlatformStats(
            Purchases: await credits.CountAsync(ct),
            TokensSold: await credits.SumAsync(x => (long)x.Delta, ct));
    }

    // ────────────────────────────────────────────────────────
    // Satın Almalar (Mobil IAP)
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Doğrulanıp kredi yüklenen IAP satın almalarını en yeniden eskiye listeler.
    /// platform: all|android|ios, environment: all|production|test (test = Sandbox/Test/Promo/Rewarded),
    /// days: son N gün (boşsa tümü), search: kullanıcı ID (GUID), e-posta parçası, Google sipariş no veya mağaza kimliği,
    /// status: all|active|refunded. Özet (adet, token, tutar, iade) limitten bağımsız olarak tüm filtrelenmiş kümeyi kapsar;
    /// tutar iade edilen satın almaları içermez.
    /// </summary>
    [HttpGet("purchases")]
    public async Task<IActionResult> ListPurchases(
        [FromQuery] string platform = "all",
        [FromQuery] string environment = "all",
        [FromQuery] int? days = null,
        [FromQuery] string? search = null,
        [FromQuery] string status = "all",
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        if (EnsureAdmin() is { } err) return err;

        var safeLimit = Math.Clamp(limit, 1, 500);
        var reasonPrefix = platform switch
        {
            "android" => "iap:android:",
            "ios" => "iap:ios:",
            _ => "iap:",
        };

        var query = db.TokenLedgers
            .AsNoTracking()
            .Where(x => x.Delta > 0 && x.Reason.StartsWith(reasonPrefix));

        if (environment == "production")
        {
            query = query.Where(x => x.MetadataJson.RootElement.GetProperty("environment").GetString() == "Production");
        }
        else if (environment == "test")
        {
            query = query.Where(x => x.MetadataJson.RootElement.GetProperty("environment").GetString() != null
                && x.MetadataJson.RootElement.GetProperty("environment").GetString() != "Production");
        }

        if (days is > 0)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-days.Value);
            query = query.Where(x => x.CreatedAt >= since);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            if (Guid.TryParse(term, out var searchUserId))
            {
                query = query.Where(x => x.UserId == searchUserId);
            }
            else
            {
                var lowerTerm = term.ToLowerInvariant();
                query = query.Where(x => x.Reason.Contains(term)
                    || x.MetadataJson.RootElement.GetProperty("orderId").GetString() == term
                    || db.UserProfiles.Any(p => p.UserId == x.UserId && p.Email != null && p.Email.ToLower().Contains(lowerTerm)));
            }
        }

        if (status is "active" or "refunded")
        {
            query = WhereRefunded(query, refunded: status == "refunded");
        }

        var totalCount = await query.CountAsync(ct);
        var totalTokens = await query.SumAsync(x => (long)x.Delta, ct);
        var refundedCount = await WhereRefunded(query, refunded: true).CountAsync(ct);
        var revenue = await SumRevenueAsync(WhereRefunded(query, refunded: false), ct);

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(safeLimit)
            .ToListAsync(ct);

        var userIds = rows.Select(x => x.UserId).Distinct().ToList();
        var profiles = await db.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        // Sayfadaki satın almaların iade ve iade geri alma satırları ("iap:" → "iap_refund:" / "iap_refund_reversed:")
        var refundReasons = rows
            .SelectMany(x => new[]
            {
                IapRefundService.RefundReasonPrefix + x.Reason[4..],
                IapRefundService.RefundReversedReasonPrefix + x.Reason[4..],
            })
            .ToList();
        var refundRows = await db.TokenLedgers
            .AsNoTracking()
            .Where(x => refundReasons.Contains(x.Reason))
            .Select(x => new { x.Reason, x.CreatedAt })
            .ToDictionaryAsync(x => x.Reason, x => x.CreatedAt, ct);

        var items = rows.Select(x =>
        {
            // reason: "iap:<platform>:<mağaza kimliği>" (Android purchaseToken, iOS transactionId)
            var parts = x.Reason.Split(':', 3);
            var meta = x.MetadataJson.RootElement;
            profiles.TryGetValue(x.UserId, out var profile);
            var fullName = profile is null ? null : $"{profile.Name} {profile.Surname}".Trim();
            DateTimeOffset? refundedAt =
                refundRows.TryGetValue(IapRefundService.RefundReasonPrefix + x.Reason[4..], out var refundAt)
                && !refundRows.ContainsKey(IapRefundService.RefundReversedReasonPrefix + x.Reason[4..])
                    ? refundAt
                    : null;

            return new AdminPurchaseResponse(
                Id: x.Id,
                CreatedAt: x.CreatedAt,
                UserId: x.UserId,
                Email: profile?.Email,
                Name: string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                Platform: parts.Length > 1 ? parts[1] : "-",
                ProductId: ReadString(meta, "productId"),
                Tokens: x.Delta,
                Environment: ReadString(meta, "environment"),
                Currency: ReadString(meta, "currency"),
                Price: ReadMilli(meta, "priceMilli"),
                Tax: ReadMilli(meta, "taxMilli"),
                Proceeds: ReadMilli(meta, "proceedsMilli"),
                Region: ReadString(meta, "regionCode") ?? ReadString(meta, "storefront"),
                OrderId: ReadString(meta, "orderId"),
                StoreReference: parts.Length > 2 ? parts[2] : x.Reason,
                RefundedAt: refundedAt);
        }).ToList();

        return Ok(new AdminPurchaseListResponse(totalCount, totalTokens, refundedCount, revenue, items));
    }

    private static string? ReadString(JsonElement root, string name)
        => root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    /// <summary>Metadata'daki ×1000 tam sayı tutarı ondalığa çevirir (priceMilli, taxMilli, proceedsMilli).</summary>
    private static decimal? ReadMilli(JsonElement root, string name)
        => root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var milli)
                ? milli / 1000m
                : null;

    // ────────────────────────────────────────────────────────
    // Token Paket Yönetimi
    // ────────────────────────────────────────────────────────

    [HttpGet("packages")]
    public async Task<IActionResult> ListPackages(CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        var packages = await db.TokenPackages
            .OrderBy(x => x.TokenAmount)
            .Select(x => new AdminPackageResponse(x.Id, x.Name, x.TokenAmount, x.PriceTry, x.Active, x.CreatedAt))
            .ToListAsync(ct);

        return Ok(packages);
    }

    [HttpPost("packages")]
    public async Task<IActionResult> CreatePackage([FromBody] AdminPackageRequest request, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        if (!ModelState.IsValid) return ValidationProblem();

        var pkg = new TokenPackage
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TokenAmount = request.TokenAmount,
            PriceTry = request.PriceTry,
            Active = request.Active,
        };

        db.TokenPackages.Add(pkg);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin: Yeni token paketi oluşturuldu. Id={Id}, Name={Name}", pkg.Id, pkg.Name);
        return Ok(new AdminPackageResponse(pkg.Id, pkg.Name, pkg.TokenAmount, pkg.PriceTry, pkg.Active, pkg.CreatedAt));
    }

    [HttpPut("packages/{id:guid}")]
    public async Task<IActionResult> UpdatePackage([FromRoute] Guid id, [FromBody] AdminPackageRequest request, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        if (!ModelState.IsValid) return ValidationProblem();

        var pkg = await db.TokenPackages.FindAsync(new object[] { id }, ct);
        if (pkg is null) return NotFound();

        pkg.Name = request.Name.Trim();
        pkg.TokenAmount = request.TokenAmount;
        pkg.PriceTry = request.PriceTry;
        pkg.Active = request.Active;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin: Token paketi güncellendi. Id={Id}", pkg.Id);
        return Ok(new AdminPackageResponse(pkg.Id, pkg.Name, pkg.TokenAmount, pkg.PriceTry, pkg.Active, pkg.CreatedAt));
    }

    [HttpDelete("packages/{id:guid}")]
    public async Task<IActionResult> DeletePackage([FromRoute] Guid id, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        var pkg = await db.TokenPackages.FindAsync(new object[] { id }, ct);
        if (pkg is null) return NotFound();

        // Aktif paketi silme — önce deaktif et
        if (pkg.Active)
        {
            return BadRequest(new { message = "Aktif paket silinemez. Önce deaktif edin." });
        }

        db.TokenPackages.Remove(pkg);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin: Token paketi silindi. Id={Id}", id);
        return NoContent();
    }

    [HttpPatch("packages/{id:guid}/toggle")]
    public async Task<IActionResult> TogglePackage([FromRoute] Guid id, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        var pkg = await db.TokenPackages.FindAsync(new object[] { id }, ct);
        if (pkg is null) return NotFound();

        pkg.Active = !pkg.Active;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin: Paket durumu değiştirildi. Id={Id}, Active={Active}", pkg.Id, pkg.Active);
        return Ok(new AdminPackageResponse(pkg.Id, pkg.Name, pkg.TokenAmount, pkg.PriceTry, pkg.Active, pkg.CreatedAt));
    }

    // ────────────────────────────────────────────────────────
    // Son Job Listesi
    // ────────────────────────────────────────────────────────

    [HttpGet("jobs")]
    public async Task<IActionResult> ListRecentJobs([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (EnsureAdmin() is { } err) return err;

        var safeLimit = Math.Clamp(limit, 1, 200);
        var jobs = await db.Jobs
            .OrderByDescending(x => x.CreatedAt)
            .Take(safeLimit)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Status,
                x.TotalIsbns,
                x.ProcessedIsbns,
                x.ConsumedTokens,
                x.ErrorMessage,
                x.CreatedAt,
                x.UpdatedAt,
            })
            .ToListAsync(ct);

        return Ok(jobs);
    }

    // ────────────────────────────────────────────────────────
    // Destek Talepleri
    // ────────────────────────────────────────────────────────

    [HttpGet("support-tickets")]
    public async Task<IActionResult> ListSupportTickets([FromQuery] string status = "open", [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (EnsureAdmin() is { } err) return err;

        var safeLimit = Math.Clamp(limit, 1, 500);
        var query = db.SupportTickets.AsQueryable();
        if (status is "open" or "resolved")
        {
            query = query.Where(x => x.Status == status);
        }

        var tickets = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(safeLimit)
            .Select(x => new AdminSupportTicketResponse(
                x.Id,
                x.UserId,
                x.Email,
                x.JobId,
                db.Jobs.Where(j => j.Id == x.JobId).Select(j => j.Status).FirstOrDefault(),
                x.Message,
                x.Status,
                x.CreatedAt,
                x.ResolvedAt))
            .ToListAsync(ct);

        return Ok(tickets);
    }

    [HttpPatch("support-tickets/{id:guid}/status")]
    public async Task<IActionResult> SetSupportTicketStatus([FromRoute] Guid id, [FromQuery] string status, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;
        if (status is not ("open" or "resolved"))
        {
            return BadRequest(new { message = "Durum 'open' veya 'resolved' olmalıdır." });
        }

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (ticket is null) return NotFound(new { message = "Destek talebi bulunamadı." });

        ticket.Status = status;
        ticket.ResolvedAt = status == "resolved" ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Destek talebi {TicketId} durumu {Status} yapıldı", id, status);
        return Ok(new { ticket.Id, ticket.Status, ticket.ResolvedAt });
    }

    // ────────────────────────────────────────────────────────
    // Hızlı Token Yükleme (Admin Kendi Hesabına)
    // ────────────────────────────────────────────────────────

    [HttpPost("me/add-tokens")]
    public async Task<IActionResult> AddTokensToMe([FromQuery] int amount, [FromServices] TokenService tokenService, CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        if (amount is <= 0 or > 100000)
        {
            return BadRequest(new { message = "Lütfen 1 ile 100,000 arasında bir değer girin." });
        }

        var userId = UserContext.GetUserIdOrThrow(User);
        await tokenService.CreditAsync(userId, null, null, amount, "admin_manual_grant", ct);
        logger.LogInformation("Admin kendi hesabına token yükledi. UserId={UserId}, Amount={Amount}", userId, amount);

        return Ok(new { success = true, addedAmount = amount });
    }

    // ────────────────────────────────────────────────────────
    // Kullanıcı Yönetimi & Başka Kullanıcılara Token Ekleme
    // ────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (EnsureAdmin() is { } err) return err;

        var safeLimit = Math.Clamp(limit, 1, 200);
        var cleanSearch = search?.Trim().ToLowerInvariant();

        IQueryable<Wallet> walletsQuery = db.Wallets.AsNoTracking();
        List<Wallet> wallets;

        if (!string.IsNullOrWhiteSpace(cleanSearch))
        {
            if (Guid.TryParse(cleanSearch, out var searchGuid))
            {
                wallets = await walletsQuery.Where(w => w.UserId == searchGuid).Take(safeLimit).ToListAsync(ct);
            }
            else
            {
                var matchingUserIds = await db.UserProfiles
                    .AsNoTracking()
                    .Where(p => (p.Email != null && p.Email.ToLower().Contains(cleanSearch))
                             || (p.Name != null && p.Name.ToLower().Contains(cleanSearch))
                             || (p.Surname != null && p.Surname.ToLower().Contains(cleanSearch)))
                    .Select(p => p.UserId)
                    .Take(safeLimit)
                    .ToListAsync(ct);

                wallets = await walletsQuery
                    .Where(w => matchingUserIds.Contains(w.UserId))
                    .Take(safeLimit)
                    .ToListAsync(ct);
            }
        }
        else
        {
            wallets = await walletsQuery
                .OrderByDescending(w => w.UpdatedAt)
                .Take(safeLimit)
                .ToListAsync(ct);
        }

        var userIds = wallets.Select(w => w.UserId).ToList();

        var profiles = await db.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var jobStats = await db.Jobs
            .AsNoTracking()
            .Where(j => userIds.Contains(j.UserId))
            .GroupBy(j => j.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalJobs = g.Count(),
                ConsumedTokens = g.Sum(x => x.ConsumedTokens)
            })
            .ToDictionaryAsync(x => x.UserId, ct);

        var result = wallets.Select(w =>
        {
            profiles.TryGetValue(w.UserId, out var p);
            jobStats.TryGetValue(w.UserId, out var js);

            var fullName = p != null
                ? $"{p.Name} {p.Surname}".Trim()
                : null;

            return new AdminUserSummaryResponse(
                UserId: w.UserId,
                Email: p?.Email,
                Name: string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                Balance: w.Balance,
                TotalJobs: js?.TotalJobs ?? 0,
                ConsumedTokens: js?.ConsumedTokens ?? 0,
                CreatedAt: w.CreatedAt,
                UpdatedAt: w.UpdatedAt
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPost("users/add-tokens")]
    public async Task<IActionResult> AddTokensToUser(
        [FromBody] AdminAddTokensRequest request,
        [FromServices] TokenService tokenService,
        CancellationToken ct)
    {
        if (EnsureAdmin() is { } err) return err;

        if (!ModelState.IsValid) return ValidationProblem();

        if (string.IsNullOrWhiteSpace(request.UserIdentifier))
        {
            return BadRequest(new { message = "Kullanıcı bilgisi (e-posta veya Kullanıcı ID) gereklidir." });
        }

        if (request.Amount <= 0 || request.Amount > 1_000_000)
        {
            return BadRequest(new { message = "Token miktarı 1 ile 1.000.000 arasında olmalıdır." });
        }

        var identifier = request.UserIdentifier.Trim();
        Guid targetUserId = Guid.Empty;
        string? targetEmail = null;
        string? targetName = null;

        // 1. E-posta ile arama
        if (identifier.Contains('@'))
        {
            var cleanEmail = identifier.ToLowerInvariant();
            var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Email != null && p.Email.ToLower() == cleanEmail, ct);
            if (profile != null)
            {
                targetUserId = profile.UserId;
                targetEmail = profile.Email;
                targetName = $"{profile.Name} {profile.Surname}".Trim();
            }
            else
            {
                // Firebase Admin SDK'dan sorgula
                try
                {
                    var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                    if (auth != null)
                    {
                        var fbUser = await auth.GetUserByEmailAsync(cleanEmail);
                        if (fbUser != null)
                        {
                            targetEmail = fbUser.Email;
                            targetName = fbUser.DisplayName;
                            if (!Guid.TryParse(fbUser.Uid, out targetUserId))
                            {
                                var hash = MD5.HashData(Encoding.UTF8.GetBytes(fbUser.Uid));
                                targetUserId = new Guid(hash);
                            }
                        }
                    }
                }
                catch (FirebaseAdmin.Auth.FirebaseAuthException)
                {
                    // Firebase'de bulunamadı
                }
            }
        }
        // 2. GUID ile arama
        else if (Guid.TryParse(identifier, out var parsedGuid))
        {
            targetUserId = parsedGuid;
            var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == targetUserId, ct);
            if (profile != null)
            {
                targetEmail = profile.Email;
                targetName = $"{profile.Name} {profile.Surname}".Trim();
            }
        }
        // 3. Firebase UID string ile arama
        else
        {
            try
            {
                var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                if (auth != null)
                {
                    var fbUser = await auth.GetUserAsync(identifier);
                    if (fbUser != null)
                    {
                        targetEmail = fbUser.Email;
                        targetName = fbUser.DisplayName;
                    }
                }
            }
            catch {}

            var hash = MD5.HashData(Encoding.UTF8.GetBytes(identifier));
            targetUserId = new Guid(hash);
        }

        if (targetUserId == Guid.Empty)
        {
            return NotFound(new { message = $"'{identifier}' ile eşleşen bir kullanıcı bulunamadı. Lütfen e-posta adresini veya Kullanıcı ID'sini kontrol edin." });
        }

        var previousBalance = await db.Wallets
            .AsNoTracking()
            .Where(x => x.UserId == targetUserId)
            .Select(x => (int?)x.Balance)
            .FirstOrDefaultAsync(ct) ?? 0;

        var adminEmail = UserContext.GetEmail(User) ?? "admin";
        var cleanReason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Admin müdahalesi / Manuel yükleme"
            : request.Reason.Trim();
        var ledgerReason = $"admin_grant: {cleanReason} (by {adminEmail})";

        await tokenService.CreditAsync(targetUserId, null, null, request.Amount, ledgerReason, ct);

        var newBalance = previousBalance + request.Amount;

        // Profil kaydı yoksa ve e-posta biliniyorsa profil taslağını oluştur
        if (!string.IsNullOrWhiteSpace(targetEmail))
        {
            var existingProfile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == targetUserId, ct);
            if (existingProfile == null)
            {
                db.UserProfiles.Add(new UserProfile
                {
                    UserId = targetUserId,
                    Email = targetEmail,
                    Name = targetName,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
                await db.SaveChangesAsync(ct);
            }
            else if (existingProfile.Email != targetEmail)
            {
                existingProfile.Email = targetEmail;
                await db.SaveChangesAsync(ct);
            }
        }

        logger.LogInformation(
            "Admin ({AdminEmail}) kullaniciya token tanımladı. TargetUserId={UserId}, Email={Email}, Amount={Amount}, Reason={Reason}, NewBalance={NewBalance}",
            adminEmail, targetUserId, targetEmail ?? "-", request.Amount, cleanReason, newBalance);

        return Ok(new AdminAddTokensResponse(
            Success: true,
            UserId: targetUserId,
            Email: targetEmail,
            PreviousBalance: previousBalance,
            AddedAmount: request.Amount,
            NewBalance: newBalance,
            Reason: cleanReason
        ));
    }
}
