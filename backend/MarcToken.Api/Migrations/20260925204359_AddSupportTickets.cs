using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcToken.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // email sütunu prod'da 20260908130000_AddEmailToUserProfile (Designer dosyası olmadığı için EF bu
            // migration'ı görmüyor) ve Program.cs'teki ALTER ile zaten var; burada yalnızca snapshot'a işleniyor.
            migrationBuilder.Sql("ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS email text;");

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_status_created_at",
                table: "support_tickets",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_user_id",
                table: "support_tickets",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_tickets");
        }
    }
}
