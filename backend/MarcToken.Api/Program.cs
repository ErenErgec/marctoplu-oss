using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using MarcToken.Api.Data;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Container PORT desteği
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

builder.Services.Configure<MarcToken.Api.Services.AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<MarcToken.Api.Services.IapOptions>(builder.Configuration.GetSection("Iap"));

// Standart ConnectionStrings veya DATABASE_URL desteği
var connectionString = builder.Configuration.GetConnectionString("Default");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Disable;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PythonJobRunner>();
builder.Services.AddScoped<JobNotificationSender>();
builder.Services.AddScoped<JobService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<GooglePlayPurchaseVerifier>();
builder.Services.AddSingleton<AppStorePurchaseVerifier>();
builder.Services.AddScoped<IapRefundService>();
builder.Services.AddHostedService<JobWorkerService>();
builder.Services.AddHostedService<FileCleanupService>();
builder.Services.AddHostedService<GuestCleanupService>();

var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
if (string.IsNullOrWhiteSpace(firebaseProjectId))
{
    throw new InvalidOperationException("Firebase:ProjectId is required for JWT validation.");
}

try
{
    var firebaseServiceAccountJson = builder.Configuration["Firebase:ServiceAccountJson"];
    var googleAppCreds = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

    if (!string.IsNullOrWhiteSpace(firebaseServiceAccountJson))
    {
        FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = GoogleCredential.FromJson(firebaseServiceAccountJson)
        });
    }
    else if (!string.IsNullOrWhiteSpace(googleAppCreds) && File.Exists(googleAppCreds))
    {
        FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = GoogleCredential.FromFile(googleAppCreds)
        });
    }
    else
    {
        FirebaseApp.Create();
    }
}
catch (Exception ex)
{
    // Firebase Admin SDK sadece FCM push bildirimleri için gereklidir.
    // JWT Authentication bağımsız çalışır. Eksik credentials durumunda backend çökmemeli.
    Console.WriteLine($"[UYARI] Firebase Admin SDK başlatılamadı: {ex.Message}. Push bildirimleri devre dışı, JWT doğrulaması aktif kalacak.");
}

var jwtIssuer = $"https://securetoken.google.com/{firebaseProjectId}";
var jwtAudience = firebaseProjectId;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtIssuer;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddMarcRateLimiting();
builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration["App:FrontendUrl"] ?? "http://localhost:3000,http://localhost:3001";
    var allowedOrigins = configuredOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
    var iyzicoApiKey = builder.Configuration["Iyzico:ApiKey"];
    var iyzicoBaseUrl = builder.Configuration["Iyzico:BaseUrl"] ?? string.Empty;
    var callbackUrl = builder.Configuration["Iyzico:CallbackUrl"] ?? string.Empty;

    // Sadece iyzico anahtarları tanımlanmışsa production kontrolü yap
    if (!string.IsNullOrWhiteSpace(iyzicoApiKey) && 
        !string.Equals(iyzicoApiKey, "sandbox-api-key", StringComparison.OrdinalIgnoreCase))
    {
        if (iyzicoBaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Production ortaminda Iyzico sandbox URL kullanilamaz.");
        }

        if (!callbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Production ortaminda Iyzico callback URL HTTPS olmalidir.");
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS email text;");
    }
    catch (Exception ex)
    {
        var startupLog = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLog.LogError(ex, "Veritabanı migration uygulanirken bir hata olustu.");
    }
}

var iapOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<IapOptions>>().Value;
if (iapOptions.IsGooglePlayConfigured)
    app.Logger.LogInformation("[IAP] Google Play doğrulaması aktif.");
else
    app.Logger.LogWarning("[IAP] Google Play doğrulaması YAPILANDIRILMAMIŞ; Android satın almaları reddedilecek. (Iap:GoogleServiceAccountPath = {Path})", iapOptions.GoogleServiceAccountPath);
if (iapOptions.IsAppStoreConfigured)
    app.Logger.LogInformation("[IAP] App Store doğrulaması aktif.");
else
    app.Logger.LogWarning("[IAP] App Store doğrulaması YAPILANDIRILMAMIŞ; iOS satın almaları reddedilecek.");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
// Docker ağındaki reverse proxy'yi (Caddy) güvenilir kabul et
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
// Kullanıcıya göre bölümlendiği için kimlik doğrulamadan sonra
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
{
    var ok = await db.Database.CanConnectAsync(ct);
    return ok ? Results.Ok(new { status = "ready" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();

app.Run();
