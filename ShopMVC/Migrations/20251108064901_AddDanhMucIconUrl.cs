using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddDanhMucIconUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "DanhMucs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 1,
                column: "IconUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 2,
                column: "IconUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 3,
                column: "IconUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 4,
                column: "IconUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 5,
                column: "IconUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "DanhMucs",
                keyColumn: "Id",
                keyValue: 6,
                column: "IconUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "DanhMucs");
        }
    }
}
