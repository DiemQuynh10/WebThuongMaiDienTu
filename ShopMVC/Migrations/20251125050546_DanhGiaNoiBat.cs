using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopMVC.Migrations
{
    /// <inheritdoc />
    public partial class DanhGiaNoiBat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LaNoiBat",
                table: "DanhGias",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaNoiBat",
                table: "DanhGias");
        }
    }
}
