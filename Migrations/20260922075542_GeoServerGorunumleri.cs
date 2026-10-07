using Microsoft.EntityFrameworkCore.Migrations;
using WebMap.Models;

#nullable disable

namespace WebMap.Migrations
{
    // GeoServer'ın okuduğu view'lar. Tablolara dokunmaz, snapshot değişmez.
    //   gs        : bütün projeler         (GeoServer workspace: webmap)
    //   gs_onayli : sadece onaylı projeler (workspace: webmap_onayli, görüntüleme rolü)
    // Ayrı şemalar: GeoServer'ın SQL kullanıcısına sadece bunlar açılır, Kullanicilar tablosunu göremez.
    // View adları iki şemada aynıdır, böylece GeoServer'da katman adları da aynı çıkar.
    // WKT -> geometry (SRID 4326). Boş WKT'li satır alınmaz: tek bozuk kayıt view'u düşürmesin.
    // Id ve ProjeId küçük harf metindir: map.js'teki id ile eşleşsin (CQL_FILTER=ProjeId='...').
    /// <inheritdoc />
    public partial class GeoServerGorunumleri : Migration
    {
        // Her temel view'un gs_onayli'da aynı adlı, sadece onaylı projeleri gösteren bir kopyası var
        static readonly string[] Gorunumler = ["Projeler", "Konutlar", "Santraller", "Fiberler", "Menholler", "Kabinler"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CREATE SCHEMA ve CREATE VIEW batch'in tek komutu olmalı: her biri ayrı Sql() çağrısı
            migrationBuilder.Sql("CREATE SCHEMA gs AUTHORIZATION dbo");
            migrationBuilder.Sql("CREATE SCHEMA gs_onayli AUTHORIZATION dbo");

            migrationBuilder.Sql("""
                CREATE VIEW gs.Projeler AS
                SELECT LOWER(CONVERT(nvarchar(36), p.Id)) AS ProjeId, p.ProjeAdi, p.Durum, p.OlusturmaTarihi,
                       geometry::STGeomFromText(p.Geometri, 4326) AS Geom
                FROM dbo.Projeler p
                WHERE p.Geometri <> ''
                """);

            migrationBuilder.Sql("""
                CREATE VIEW gs.Konutlar AS
                SELECT LOWER(CONVERT(nvarchar(36), k.Id)) AS Id, LOWER(CONVERT(nvarchar(36), k.ProjeId)) AS ProjeId, p.Durum,
                       k.UAVTKod, k.BBKsayi,
                       geometry::STGeomFromText(k.Geometri, 4326) AS Geom
                FROM dbo.Konutlar k
                JOIN dbo.Projeler p ON p.Id = k.ProjeId
                WHERE k.Geometri <> ''
                """);

            migrationBuilder.Sql("""
                CREATE VIEW gs.Santraller AS
                SELECT LOWER(CONVERT(nvarchar(36), s.Id)) AS Id, LOWER(CONVERT(nvarchar(36), s.ProjeId)) AS ProjeId, p.Durum,
                       s.Kod, s.Kapasite,
                       geometry::STGeomFromText(s.Geometri, 4326) AS Geom
                FROM dbo.Santraller s
                JOIN dbo.Projeler p ON p.Id = s.ProjeId
                WHERE s.Geometri <> ''
                """);

            migrationBuilder.Sql("""
                CREATE VIEW gs.Fiberler AS
                SELECT LOWER(CONVERT(nvarchar(36), f.Id)) AS Id, LOWER(CONVERT(nvarchar(36), f.ProjeId)) AS ProjeId, p.Durum,
                       geometry::STGeomFromText(f.Guzergah, 4326) AS Geom
                FROM dbo.Fiberler f
                JOIN dbo.Projeler p ON p.Id = f.ProjeId
                WHERE f.Guzergah <> ''
                """);

            // Menhol ve Kabin (TPT): Konum ve ProjeId taban tabloda (NetworkElements)
            migrationBuilder.Sql("""
                CREATE VIEW gs.Menholler AS
                SELECT LOWER(CONVERT(nvarchar(36), m.Id)) AS Id, LOWER(CONVERT(nvarchar(36), n.ProjeId)) AS ProjeId, p.Durum,
                       m.Kod, m.Derinlik,
                       geometry::STGeomFromText(n.Konum, 4326) AS Geom
                FROM dbo.Menholler m
                JOIN dbo.NetworkElements n ON n.Id = m.Id
                JOIN dbo.Projeler p ON p.Id = n.ProjeId
                WHERE n.Konum <> ''
                """);

            migrationBuilder.Sql("""
                CREATE VIEW gs.Kabinler AS
                SELECT LOWER(CONVERT(nvarchar(36), k.Id)) AS Id, LOWER(CONVERT(nvarchar(36), n.ProjeId)) AS ProjeId, p.Durum,
                       k.Kod, k.KabinTipi, k.KabinKapasitesi,
                       geometry::STGeomFromText(n.Konum, 4326) AS Geom
                FROM dbo.Kabinler k
                JOIN dbo.NetworkElements n ON n.Id = k.Id
                JOIN dbo.Projeler p ON p.Id = n.ProjeId
                WHERE n.Konum <> ''
                """);

            foreach (var ad in Gorunumler)
                migrationBuilder.Sql($"CREATE VIEW gs_onayli.{ad} AS SELECT * FROM gs.{ad} WHERE Durum = '{ProjeDurumlari.Onaylandi}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Onaylı view'lar temel view'lara bağlı: önce onlar silinir
            foreach (var ad in Gorunumler)
                migrationBuilder.Sql($"DROP VIEW gs_onayli.{ad}");
            foreach (var ad in Gorunumler)
                migrationBuilder.Sql($"DROP VIEW gs.{ad}");
            migrationBuilder.Sql("DROP SCHEMA gs_onayli");
            migrationBuilder.Sql("DROP SCHEMA gs");
        }
    }
}
