namespace WebMap.Models
{
    // Proje: agin cizilecegi calisma alani. Kendi sinir poligonu var (Konut gibi WKT).
    // Diger tum nesneler (Menhol/Kabin/Santral/Konut/Fiber) ProjeId ile buna baglanir.
    public class Proje
    {
        public Guid Id { get; set; }
        public string Ad { get; set; } = "";

        // Proje sinirlari, WKT metni olarak tutulur. Ornek: "POLYGON((32.86 39.95, ...))".
        public string Geometri { get; set; } = "";

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }
}
