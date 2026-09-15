namespace WebMap.Models
{
    // Agdaki nokta tipli elemanlarin ortak tabani.
    // TPT: bu sinif "NetworkElements" tablosuna, alt tipler kendi tablolarina yazilir.
    // abstract -> dogrudan "NetworkElement" ornegi olusturulamaz, taban tabloda basibos satir kalmaz.
    public abstract class NetworkElement
    {
        // Guid: turden bagimsiz global benzersiz Id (bina ile menhol ayni Id'yi alamaz).
        public Guid Id { get; set; }

        // Konum, WKT metni olarak tutulur. Ornek: "POINT(32.866 39.959)".
        public string Konum { get; set; } = "";

        // Bu nesne hangi projeye ait (Proje.Id). FK yok - sunucu tarafinda kontrol edilir.
        public Guid ProjeId { get; set; }
    }
}
