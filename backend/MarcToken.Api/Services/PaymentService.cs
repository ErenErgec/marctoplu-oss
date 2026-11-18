using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarcToken.Api.Contracts;
using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MarcToken.Api.Services;

public sealed class PaymentService(AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory, TokenService tokenService)
{
    private readonly string _baseUrl = (config["Iyzico:BaseUrl"] ?? "https://sandbox-api.iyzipay.com").TrimEnd('/');
    private readonly string _callbackUrl = config["Iyzico:CallbackUrl"] ?? "http://localhost:8080/api/payments/iyzico/callback";
    private readonly string _apiKey = config["Iyzico:ApiKey"] ?? string.Empty;
    private readonly string _secretKey = config["Iyzico:SecretKey"] ?? string.Empty;

    public async Task<List<TokenPackage>> GetActivePackagesAsync(CancellationToken ct = default)
        => await db.TokenPackages.Where(x => x.Active).OrderBy(x => x.TokenAmount).ToListAsync(ct);

    public async Task<(Payment payment, string checkoutUrl)> InitCheckoutAsync(Guid userId, string? email, CheckoutInitRequest request, CancellationToken ct = default)
    {
        var package = await db.TokenPackages.FirstOrDefaultAsync(x => x.Id == request.PackageId && x.Active, ct)
            ?? throw new InvalidOperationException("Token package not found.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageId = package.Id,
            ProviderConversationId = Guid.NewGuid().ToString("N"),
            AmountTry = package.PriceTry,
            TokenAmount = package.TokenAmount,
            Status = "pending",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var payloadObj = new
        {
            locale = "tr",
            conversationId = payment.ProviderConversationId,
            price = package.PriceTry.ToString("0.00", CultureInfo.InvariantCulture),
            paidPrice = package.PriceTry.ToString("0.00", CultureInfo.InvariantCulture),
            currency = "TRY",
            basketId = payment.Id.ToString(),
            paymentGroup = "PRODUCT",
            callbackUrl = _callbackUrl,
            enabledInstallments = new[] { 1 },
            buyer = new
            {
                id = userId.ToString(),
                name = request.Buyer.Name,
                surname = request.Buyer.Surname,
                email = email ?? "user@example.com",
                identityNumber = request.Buyer.IdentityNumber,
                registrationAddress = request.Buyer.Address,
                ip = "127.0.0.1",
                city = request.Buyer.City,
                country = request.Buyer.Country,
                zipCode = request.Buyer.ZipCode,
                gsmNumber = request.Buyer.GsmNumber,
            },
            shippingAddress = new
            {
                contactName = $"{request.Buyer.Name} {request.Buyer.Surname}",
                city = request.Buyer.City,
                country = request.Buyer.Country,
                address = request.Buyer.Address,
                zipCode = request.Buyer.ZipCode,
            },
            billingAddress = new
            {
                contactName = $"{request.Buyer.Name} {request.Buyer.Surname}",
                city = request.Buyer.City,
                country = request.Buyer.Country,
                address = request.Buyer.Address,
                zipCode = request.Buyer.ZipCode,
            },
            basketItems = new[]
            {
                new
                {
                    id = package.Id.ToString(),
                    name = package.Name,
                    category1 = "Tokens",
                    itemType = "VIRTUAL",
                    price = package.PriceTry.ToString("0.00", CultureInfo.InvariantCulture),
                }
            },
        };

        var payload = JsonSerializer.Serialize(payloadObj);
        var response = await SendIyzicoRequestAsync(
            HttpMethod.Post,
            "/payment/iyzipos/checkoutform/initialize/auth/ecom",
            payload,
            ct);

        var status = response.GetProperty("status").GetString();
        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var error = response.TryGetProperty("errorMessage", out var e) ? e.GetString() : "Unknown error";
            throw new InvalidOperationException($"Iyzico checkout init failed: {error}");
        }

        var checkoutUrl = response.GetProperty("paymentPageUrl").GetString();
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("Iyzico response did not include paymentPageUrl.");
        }

        payment.RawPayload = response.GetRawText();
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return (payment, checkoutUrl);
    }

    public async Task<Payment> FinalizeAndCreditCheckoutAsync(string token, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { locale = "tr", token });
        var response = await SendIyzicoRequestAsync(
            HttpMethod.Post,
            "/payment/iyzipos/checkoutform/auth/ecom/detail",
            payload,
            ct);

        var status = response.GetProperty("status").GetString();
        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var error = response.TryGetProperty("errorMessage", out var e) ? e.GetString() : "Unknown error";
            throw new InvalidOperationException($"Iyzico callback verification failed: {error}");
        }

        var paymentStatus = response.TryGetProperty("paymentStatus", out var ps) ? ps.GetString() : null;
        if (!string.Equals(paymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Iyzico payment status is not SUCCESS.");
        }

        var currency = response.TryGetProperty("currency", out var c) ? c.GetString() : null;
        if (!string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unexpected currency in callback.");
        }

        var conversationId = response.TryGetProperty("conversationId", out var conv) ? conv.GetString() : null;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new InvalidOperationException("Missing conversationId in callback response.");
        }

        var payment = await db.Payments.FirstOrDefaultAsync(x => x.ProviderConversationId == conversationId, ct)
            ?? throw new InvalidOperationException("Payment record not found.");

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        payment = await db.Payments
            .FromSqlInterpolated($"select * from payments where id = {payment.Id} for update")
            .FirstAsync(ct);

        if (payment.Status == "paid")
        {
            await tx.CommitAsync(ct);
            return payment;
        }

        var paidPriceRaw = response.TryGetProperty("paidPrice", out var paidEl) ? paidEl.GetString() : null;
        if (!decimal.TryParse(paidPriceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var paidAmount))
        {
            throw new InvalidOperationException("Invalid paidPrice in callback response.");
        }

        if (Math.Abs(paidAmount - payment.AmountTry) > 0.01m)
        {
            throw new InvalidOperationException("Paid amount mismatch.");
        }

        payment.Status = "paid";
        payment.ProviderPaymentId = response.TryGetProperty("paymentId", out var pid) ? pid.GetString() : null;
        payment.PaidAmountTry = paidAmount;
        payment.VerifiedAt = DateTimeOffset.UtcNow;
        payment.RawVerification = response.GetRawText();
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        await tokenService.CreditAsync(payment.UserId, payment.Id, null, payment.TokenAmount, "payment_credit", ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    private async Task<JsonElement> SendIyzicoRequestAsync(HttpMethod method, string uriPath, string jsonBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_secretKey))
        {
            throw new InvalidOperationException("Iyzico credentials are not configured.");
        }

        var randomKey = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var signaturePayload = string.Concat(randomKey, uriPath, jsonBody);
        var signature = ComputeHmacSha256Hex(signaturePayload, _secretKey);
        var authorizationRaw = $"apiKey:{_apiKey}&randomKey:{randomKey}&signature:{signature}";
        var authorizationEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationRaw));

        var request = new HttpRequestMessage(method, $"{_baseUrl}{uriPath}")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"IYZWSv2 {authorizationEncoded}");
        request.Headers.TryAddWithoutValidation("x-iyzi-rnd", randomKey);

        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Iyzico HTTP error ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }

    private static string ComputeHmacSha256Hex(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
