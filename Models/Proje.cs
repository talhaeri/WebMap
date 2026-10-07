namespace WebMap.Models
{
    // Ağın çizildiği çalışma alanı. Menhol, Kabin, Santral, Konut ve Fiber ProjeId ile buna bağlanır.
    public class Proje
    {
        public Guid Id { get; set; }
        public string ProjeAdi { get; set; } = "";

        // Sınır poligonu, WKT metni. Örnek: "POLYGON((32.86 39.95, ...))"
        public string Geometri { get; set; } = "";

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public string Durum { get; set; } = ProjeDurumlari.Planlama;
        public Guid? OlusturanId { get; set; }
        public string? OlusturanAdi { get; set; }

        // Geçerli onayın bilgileri; proje Planlama'ya alınınca temizlenir
        public Guid? OnaylayanId { get; set; }
        public string? OnaylayanAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
        public string? OnayMaliyeti { get; set; }   // onay anındaki MaliyetSonucu (JSON)

        public string? RedNotu { get; set; }        // son reddin gerekçesi
    }
}
