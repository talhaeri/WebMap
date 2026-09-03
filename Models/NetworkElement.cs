namespace WebMap.Models
{
    // Agdaki nokta tipli elemanlarin ortak tabani.
    // TPT: bu sinif "NetworkElements" tablosuna, alt tipler kendi tablolarina yazilir.
    // abstract -> dogrudan "NetworkElement" ornegi olusturulamaz, taban tabloda basibos satir kalmaz.
    public abstract class NetworkElement
    {
        public int Id { get; set; }

        // Konum, WKT metni olarak tutulur. Ornek: "POINT(32.866 39.959)".
        public string Konum { get; set; } = "";
    }
}
