using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantParentToSanPham : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplaySuffix",
                table: "SanPhams",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "SanPhams",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdDanhMuc",
                table: "SanPhams",
                column: "IdDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_ParentId",
                table: "SanPhams",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SanPhams_IdDanhMuc",
                table: "SanPhams");

            migrationBuilder.DropIndex(
                name: "IX_SanPhams_ParentId",
                table: "SanPhams");

            migrationBuilder.DropColumn(
                name: "DisplaySuffix",
                table: "SanPhams");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "SanPhams");
        }
    }
}
