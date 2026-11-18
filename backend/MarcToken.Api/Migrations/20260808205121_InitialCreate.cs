using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MarcToken.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_isbns = table.Column<int>(type: "integer", nullable: false),
                    processed_isbns = table.Column<int>(type: "integer", nullable: false),
                    estimated_tokens = table.Column<int>(type: "integer", nullable: false),
                    consumed_tokens = table.Column<int>(type: "integer", nullable: false),
                    refunded_tokens = table.Column<int>(type: "integer", nullable: false),
                    input_file_path = table.Column<string>(type: "text", nullable: false),
                    output_file_path = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_conversation_id = table.Column<string>(type: "text", nullable: false),
                    provider_payment_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    amount_try = table.Column<decimal>(type: "numeric", nullable: false),
                    paid_amount_try = table.Column<decimal>(type: "numeric", nullable: true),
                    token_amount = table.Column<int>(type: "integer", nullable: false),
                    raw_payload = table.Column<string>(type: "text", nullable: true),
                    raw_verification = table.Column<string>(type: "text", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "token_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delta = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_ledger", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "token_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    token_amount = table.Column<int>(type: "integer", nullable: false),
                    price_try = table.Column<decimal>(type: "numeric", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    surname = table.Column<string>(type: "text", nullable: true),
                    identity_number = table.Column<string>(type: "text", nullable: true),
                    gsm_number = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    zip_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.user_id);
                });

            migrationBuilder.InsertData(
                table: "token_packages",
                columns: new[] { "id", "active", "created_at", "name", "price_try", "token_amount" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8729), new TimeSpan(0, 0, 0, 0, 0)), "100 Token", 199.90m, 100 },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8743), new TimeSpan(0, 0, 0, 0, 0)), "500 Token", 799.90m, 500 },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8745), new TimeSpan(0, 0, 0, 0, 0)), "2000 Token", 2499.90m, 2000 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "token_ledger");

            migrationBuilder.DropTable(
                name: "token_packages");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
