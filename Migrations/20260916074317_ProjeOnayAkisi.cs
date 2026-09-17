using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMap.Migrations
{
    /// <inheritdoc />
    public partial class ProjeOnayAkisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ad -> ProjeAdi: kolon SILINMIYOR, yeniden adlandiriliyor ki mevcut proje adlari kalsin.
            migrationBuilder.RenameColumn(
                name: "Ad",
                table: "Projeler",
                newName: "ProjeAdi");

            // Yeniden adlandirilan kolon nvarchar(max) gelir; modeldeki 100 sinirina cekiliyor.
            migrationBuilder.AlterColumn<string>(
                name: "ProjeAdi",
                table: "Projeler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Durum",
                table: "Projeler",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Planlama");

            migrationBuilder.AddColumn<string>(
                name: "OlusturanAdi",
                table: "Projeler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OlusturanId",
                table: "Projeler",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnayMaliyeti",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnayTarihi",
                table: "Projeler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanAdi",
                table: "Projeler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OnaylayanId",
                table: "Projeler",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedNotu",
                table: "Projeler",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjeGecmisi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjeAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Islem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KullaniciId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Not = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaliyetJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjeGecmisi", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Proje_Durum",
                table: "Projeler",
                sql: "Durum IN ('Planlama', 'OnayBekliyor', 'Onaylandi')");

            migrationBuilder.CreateIndex(
                name: "IX_ProjeGecmisi_ProjeId",
                table: "ProjeGecmisi",
                column: "ProjeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjeGecmisi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Proje_Durum",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OlusturanAdi",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OlusturanId",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OnayMaliyeti",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OnayTarihi",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OnaylayanAdi",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "OnaylayanId",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "RedNotu",
                table: "Projeler");

            migrationBuilder.AlterColumn<string>(
                name: "ProjeAdi",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.RenameColumn(
                name: "ProjeAdi",
                table: "Projeler",
                newName: "Ad");
        }
    }
}
