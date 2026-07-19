using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CodeShareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Type" },
                values: new object[,]
                {
                    { 5, "Python", "NgonNgu" },
                    { 6, "C / C++", "NgonNgu" },
                    { 7, "Go", "NgonNgu" },
                    { 8, "Ruby", "NgonNgu" },
                    { 9, "Swift", "NgonNgu" },
                    { 10, "Kotlin", "NgonNgu" },
                    { 11, "Rust", "NgonNgu" },
                    { 12, "TypeScript", "NgonNgu" },
                    { 13, "SQL", "NgonNgu" },
                    { 14, "Dart / Flutter", "NgonNgu" },
                    { 15, "Ngôn ngữ Khác", "NgonNgu" },
                    { 16, "Nhập môn lập trình", "MonHoc" },
                    { 17, "Kỹ thuật lập trình", "MonHoc" },
                    { 18, "Lập trình hướng đối tượng (OOP)", "MonHoc" },
                    { 19, "Cấu trúc dữ liệu và giải thuật", "MonHoc" },
                    { 20, "Cơ sở dữ liệu", "MonHoc" },
                    { 21, "Phân tích thiết kế hệ thống", "MonHoc" },
                    { 22, "Phát triển ứng dụng Web", "MonHoc" },
                    { 23, "Phát triển ứng dụng Di động", "MonHoc" },
                    { 24, "Trí tuệ nhân tạo / Học máy", "MonHoc" },
                    { 25, "Đồ án / Khóa luận tốt nghiệp", "MonHoc" },
                    { 26, "Môn học Khác", "MonHoc" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26);
        }
    }
}
