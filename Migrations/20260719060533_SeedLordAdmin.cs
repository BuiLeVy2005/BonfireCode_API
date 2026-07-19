using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeShareAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedLordAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "Bio", "CoverUrl", "CreatedAt", "Email", "FullName", "PasswordHash", "RankId", "ResetToken", "ResetTokenExpiry", "Role", "SelectedBannerUrl", "SelectedBorderUrl", "TotalEmbers", "Username" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "https://placehold.co/400x400/0a0a0c/d4af37?text=LordAdmin", "Người duy trì trật tự của BonfireCode.", "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@bonfirecode.com", "Tòa Án Tối Cao", "$2a$11$T2bGDzWt7VtIJerTTo2gIuoXReuPtF51Mu9bovy4aAk50bLaI0jxG", 5, null, null, "Admin", "", "", 9999, "LordAdmin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
