using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcToken.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "user_profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "user_profiles");
        }
    }
}
