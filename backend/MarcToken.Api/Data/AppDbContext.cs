using Microsoft.EntityFrameworkCore;

namespace MarcToken.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<TokenPackage> TokenPackages => Set<TokenPackage>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<TokenLedger> TokenLedgers => Set<TokenLedger>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<DeviceTrial> DeviceTrials => Set<DeviceTrial>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>().ToTable("wallets").HasKey(x => x.UserId);
        modelBuilder.Entity<TokenPackage>().ToTable("token_packages");
        modelBuilder.Entity<Payment>().ToTable("payments");
        modelBuilder.Entity<Job>().ToTable("jobs");
        modelBuilder.Entity<TokenLedger>().ToTable("token_ledger");
        modelBuilder.Entity<UserProfile>().ToTable("user_profiles").HasKey(x => x.UserId);
        modelBuilder.Entity<DeviceTrial>().ToTable("device_trials").HasKey(x => x.Id);
        modelBuilder.Entity<DeviceTrial>().HasIndex(x => x.DeviceId).IsUnique();

        modelBuilder.Entity<Wallet>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Wallet>().Property(x => x.Balance).HasColumnName("balance");
        modelBuilder.Entity<Wallet>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<Wallet>().Property(x => x.UpdatedAt).HasColumnName("updated_at");

        modelBuilder.Entity<TokenPackage>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<TokenPackage>().Property(x => x.Name).HasColumnName("name");
        modelBuilder.Entity<TokenPackage>().Property(x => x.TokenAmount).HasColumnName("token_amount");
        modelBuilder.Entity<TokenPackage>().Property(x => x.PriceTry).HasColumnName("price_try");
        modelBuilder.Entity<TokenPackage>().Property(x => x.Active).HasColumnName("active");
        modelBuilder.Entity<TokenPackage>().Property(x => x.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<Payment>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<Payment>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Payment>().Property(x => x.PackageId).HasColumnName("package_id");
        modelBuilder.Entity<Payment>().Property(x => x.Provider).HasColumnName("provider");
        modelBuilder.Entity<Payment>().Property(x => x.ProviderConversationId).HasColumnName("provider_conversation_id");
        modelBuilder.Entity<Payment>().Property(x => x.ProviderPaymentId).HasColumnName("provider_payment_id");
        modelBuilder.Entity<Payment>().Property(x => x.Status).HasColumnName("status");
        modelBuilder.Entity<Payment>().Property(x => x.AmountTry).HasColumnName("amount_try");
        modelBuilder.Entity<Payment>().Property(x => x.PaidAmountTry).HasColumnName("paid_amount_try");
        modelBuilder.Entity<Payment>().Property(x => x.TokenAmount).HasColumnName("token_amount");
        modelBuilder.Entity<Payment>().Property(x => x.RawPayload).HasColumnName("raw_payload");
        modelBuilder.Entity<Payment>().Property(x => x.RawVerification).HasColumnName("raw_verification");
        modelBuilder.Entity<Payment>().Property(x => x.VerifiedAt).HasColumnName("verified_at");
        modelBuilder.Entity<Payment>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<Payment>().Property(x => x.UpdatedAt).HasColumnName("updated_at");

        modelBuilder.Entity<Job>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<Job>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Job>().Property(x => x.Status).HasColumnName("status");
        modelBuilder.Entity<Job>().Property(x => x.TotalIsbns).HasColumnName("total_isbns");
        modelBuilder.Entity<Job>().Property(x => x.ProcessedIsbns).HasColumnName("processed_isbns");
        modelBuilder.Entity<Job>().Property(x => x.EstimatedTokens).HasColumnName("estimated_tokens");
        modelBuilder.Entity<Job>().Property(x => x.ConsumedTokens).HasColumnName("consumed_tokens");
        modelBuilder.Entity<Job>().Property(x => x.RefundedTokens).HasColumnName("refunded_tokens");
        modelBuilder.Entity<Job>().Property(x => x.InputFilePath).HasColumnName("input_file_path");
        modelBuilder.Entity<Job>().Property(x => x.OutputFilePath).HasColumnName("output_file_path");
        modelBuilder.Entity<Job>().Property(x => x.ErrorMessage).HasColumnName("error_message");
        modelBuilder.Entity<Job>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<Job>().Property(x => x.UpdatedAt).HasColumnName("updated_at");

        modelBuilder.Entity<TokenLedger>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<TokenLedger>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<TokenLedger>().Property(x => x.PaymentId).HasColumnName("payment_id");
        modelBuilder.Entity<TokenLedger>().Property(x => x.JobId).HasColumnName("job_id");
        modelBuilder.Entity<TokenLedger>().Property(x => x.Delta).HasColumnName("delta");
        modelBuilder.Entity<TokenLedger>().Property(x => x.Reason).HasColumnName("reason");
        modelBuilder.Entity<TokenLedger>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<TokenLedger>().Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        modelBuilder.Entity<UserProfile>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserProfile>().Property(x => x.Email).HasColumnName("email");
        modelBuilder.Entity<UserProfile>().Property(x => x.Name).HasColumnName("name");
        modelBuilder.Entity<UserProfile>().Property(x => x.Surname).HasColumnName("surname");
        modelBuilder.Entity<UserProfile>().Property(x => x.IdentityNumber).HasColumnName("identity_number");
        modelBuilder.Entity<UserProfile>().Property(x => x.GsmNumber).HasColumnName("gsm_number");
        modelBuilder.Entity<UserProfile>().Property(x => x.City).HasColumnName("city");
        modelBuilder.Entity<UserProfile>().Property(x => x.Country).HasColumnName("country");
        modelBuilder.Entity<UserProfile>().Property(x => x.Address).HasColumnName("address");
        modelBuilder.Entity<UserProfile>().Property(x => x.ZipCode).HasColumnName("zip_code");
        modelBuilder.Entity<UserProfile>().Property(x => x.FcmToken).HasColumnName("fcm_token");
        modelBuilder.Entity<UserProfile>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<UserProfile>().Property(x => x.UpdatedAt).HasColumnName("updated_at");

        modelBuilder.Entity<DeviceTrial>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<DeviceTrial>().Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(128);
        modelBuilder.Entity<DeviceTrial>().Property(x => x.FirstUserId).HasColumnName("first_user_id");
        modelBuilder.Entity<DeviceTrial>().Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        modelBuilder.Entity<DeviceTrial>().Property(x => x.ClaimedAt).HasColumnName("claimed_at");

        modelBuilder.Entity<SupportTicket>().ToTable("support_tickets").HasKey(x => x.Id);
        modelBuilder.Entity<SupportTicket>().HasIndex(x => new { x.Status, x.CreatedAt });
        modelBuilder.Entity<SupportTicket>().HasIndex(x => x.UserId);
        modelBuilder.Entity<SupportTicket>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<SupportTicket>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<SupportTicket>().Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
        modelBuilder.Entity<SupportTicket>().Property(x => x.JobId).HasColumnName("job_id");
        modelBuilder.Entity<SupportTicket>().Property(x => x.Message).HasColumnName("message").HasMaxLength(4000);
        modelBuilder.Entity<SupportTicket>().Property(x => x.Status).HasColumnName("status").HasMaxLength(16);
        modelBuilder.Entity<SupportTicket>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<SupportTicket>().Property(x => x.ResolvedAt).HasColumnName("resolved_at");

        modelBuilder.Entity<TokenPackage>().HasData(
            new TokenPackage { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "100 Token", TokenAmount = 100, PriceTry = 199.90m, Active = true },
            new TokenPackage { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "500 Token", TokenAmount = 500, PriceTry = 799.90m, Active = true },
            new TokenPackage { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "2000 Token", TokenAmount = 2000, PriceTry = 2499.90m, Active = true }
        );
    }
}
