using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcToken.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFcmToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fcm_token",
                table: "user_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 9, 11, 31, 29, 351, DateTimeKind.Unspecified).AddTicks(1155), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 9, 11, 31, 29, 351, DateTimeKind.Unspecified).AddTicks(1174), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 9, 11, 31, 29, 351, DateTimeKind.Unspecified).AddTicks(1175), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fcm_token",
                table: "user_profiles");

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8729), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8743), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "token_packages",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 8, 8, 20, 51, 20, 798, DateTimeKind.Unspecified).AddTicks(8745), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
