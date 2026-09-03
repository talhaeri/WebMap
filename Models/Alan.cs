namespace WebMap.Models
{
    // Poligon tipli elemanlarin ORTAK TABANI. abstract: dogrudan "Alan" cizilmez/kaydedilmez;
    // sadece Konut ve Ticari bu tabandan turer.
    // TPT: bu sinif "Alanlar" tablosuna (Id + Geometri), alt tipler "Konutlar" / "Ticariler".
    public abstract class Alan
    {
        public int Id { get; set; }

        // Sinir geometrisi, WKT metni olarak tutulur. Ornek: "POLYGON((32.86 39.95, ...))".
        public string Geometri { get; set; } = "";
    }
}
