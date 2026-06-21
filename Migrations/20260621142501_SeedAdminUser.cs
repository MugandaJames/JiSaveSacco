using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiSaveSacco.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "PasswordHash", "Role", "Status", "Username" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "$2a$11$.Ia6nRXBNq17hDpmLdZ.6eqERdAkO17mYcU9RdjC44g7eqrMoSmx.", "Admin", "Active", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);
        }
    }
}
