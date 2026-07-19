using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CodeShareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RankId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalEmbers",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmberTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmberTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmberTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ranks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredEmbers = table.Column<int>(type: "int", nullable: false),
                    SvgIcon = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ranks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Ranks",
                columns: new[] { "Id", "Name", "RequiredEmbers", "SvgIcon" },
                values: new object[,]
                {
                    { 1, "Kẻ Lưu Đày", 0, "rank1" },
                    { 2, "Kẻ Nhóm Lửa", 100, "rank2" },
                    { 3, "Kỵ Sĩ Thuật Toán", 500, "rank3" },
                    { 4, "Ma Tôn Dữ Liệu", 2000, "rank4" },
                    { 5, "Lãnh Chúa Tro Tàn", 5000, "rank5" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RankId",
                table: "Users",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_EmberTransactions_UserId",
                table: "EmberTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Ranks_RankId",
                table: "Users",
                column: "RankId",
                principalTable: "Ranks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Ranks_RankId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "EmberTransactions");

            migrationBuilder.DropTable(
                name: "Ranks");

            migrationBuilder.DropIndex(
                name: "IX_Users_RankId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalEmbers",
                table: "Users");
        }
    }
}
