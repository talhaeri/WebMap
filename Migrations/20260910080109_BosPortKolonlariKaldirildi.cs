using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMap.Migrations
{
    /// <inheritdoc />
    public partial class BosPortKolonlariKaldirildi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BosPort",
                table: "Santraller");

            migrationBuilder.DropColumn(
                name: "BosPort",
                table: "Kabinler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BosPort",
                table: "Santraller",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BosPort",
                table: "Kabinler",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
