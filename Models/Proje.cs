using static WebMap.Models.ProjeSabitler;

namespace WebMap.Models
{
    // Proje: agin cizilecegi calisma alani. Kendi sinir poligonu var (Konut gibi WKT).
    // Diger tum nesneler (Menhol/Kabin/Santral/Konut/Fiber) ProjeId ile buna baglanir.
    public class Proje
    {
        public Guid Id { get; set; }
        public string ProjeAdi { get; set; } = "";

        // Proje sinirlari, WKT metni olarak tutulur. Ornek: "POLYGON((32.86 39.95, ...))".
        public string Geometri { get; set; } = "";

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public string Durum { get; set; } = ProjeDurumlari.Planlama;
        public Guid? OlusturanId { get; set; }
        public string? OlusturanAdi { get; set; }
        public Guid? OnaylayanId { get; set; }
        public string? OnaylayanAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
        public string? OnayMaliyeti { get; set; }   // MaliyetSonucu JSON
        public string? RedNotu { get; set; }
    }
}
