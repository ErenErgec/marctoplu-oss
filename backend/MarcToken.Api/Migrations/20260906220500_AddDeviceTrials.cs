using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcToken.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTrials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_trials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_trials", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_trials_device_id",
                table: "device_trials",
                column: "device_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_trials");
        }
    }
}
