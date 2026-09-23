using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmedAndVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId_CreatedAtUtc",
                table: "PasswordResetTokens");

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "PasswordResetTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "PasswordResetTokens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PasswordReset");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId_Purpose_CreatedAtUtc",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "Purpose", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId_Purpose_CreatedAtUtc",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "PasswordResetTokens");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId_CreatedAtUtc",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }
    }
}
