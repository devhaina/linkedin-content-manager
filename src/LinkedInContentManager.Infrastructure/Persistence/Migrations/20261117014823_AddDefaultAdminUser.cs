using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkedInContentManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAtUtc", "DisplayName", "Email", "IsActive", "PasswordHash", "RefreshTokenExpiresAtUtc", "RefreshTokenHash", "Role", "UpdatedAtUtc" },
                values: new object[] { new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default Admin", "admin@linkedin-content-manager.local", true, "SEED$NOT_A_REAL_HASH$REPLACED_IN_PHASE_6", null, null, "Admin", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000001"));
        }
    }
}
