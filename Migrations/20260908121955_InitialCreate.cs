using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebMap.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BirimMaliyetler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NesneTuru = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Iscilik = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Malzeme = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimMaliyetler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fiberler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Guzergah = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaslangicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BitisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fiberler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Konutlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Geometri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UAVTKod = table.Column<long>(type: "bigint", nullable: false),
                    BBKsayi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konutlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Konum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkElements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Geometri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Santraller",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Geometri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kapasite = table.Column<int>(type: "int", nullable: false),
                    BosPort = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Santraller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kabinler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KabinTipi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KabinKapasitesi = table.Column<int>(type: "int", nullable: false),
                    BosPort = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kabinler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kabinler_NetworkElements_Id",
                        column: x => x.Id,
                        principalTable: "NetworkElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menholler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Derinlik = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menholler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menholler_NetworkElements_Id",
                        column: x => x.Id,
                        principalTable: "NetworkElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BirimMaliyetler",
                columns: new[] { "Id", "Iscilik", "Malzeme", "NesneTuru" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 2000m, 3000m, "Menhol" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 4000m, 6000m, "Kabin" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 20000m, 30000m, "Santral" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 15m, 25m, "Fiber" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BirimMaliyetler_NesneTuru",
                table: "BirimMaliyetler",
                column: "NesneTuru",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BirimMaliyetler");

            migrationBuilder.DropTable(
                name: "Fiberler");

            migrationBuilder.DropTable(
                name: "Kabinler");

            migrationBuilder.DropTable(
                name: "Konutlar");

            migrationBuilder.DropTable(
                name: "Menholler");

            migrationBuilder.DropTable(
                name: "Projeler");

            migrationBuilder.DropTable(
                name: "Santraller");

            migrationBuilder.DropTable(
                name: "NetworkElements");
        }
    }
}
