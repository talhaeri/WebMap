using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMap.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Geometri = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alanlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fiberler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Guzergah = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaslangicId = table.Column<int>(type: "int", nullable: false),
                    BitisId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fiberler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Konum = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkElements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Konutlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UAVTKod = table.Column<int>(type: "int", nullable: false),
                    BBKsayi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konutlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Konutlar_Alanlar_Id",
                        column: x => x.Id,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ticariler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UAVTKod = table.Column<int>(type: "int", nullable: false),
                    IsyeriSayisi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ticariler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ticariler_Alanlar_Id",
                        column: x => x.Id,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kabinler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
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
                    Id = table.Column<int>(type: "int", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fiberler");

            migrationBuilder.DropTable(
                name: "Kabinler");

            migrationBuilder.DropTable(
                name: "Konutlar");

            migrationBuilder.DropTable(
                name: "Menholler");

            migrationBuilder.DropTable(
                name: "Ticariler");

            migrationBuilder.DropTable(
                name: "NetworkElements");

            migrationBuilder.DropTable(
                name: "Alanlar");
        }
    }
}
