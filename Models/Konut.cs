namespace WebMap.Models
{
    // Konut amacli bina (poligon). Tek poligon tipi kaldigi icin ayri bir taban sinif yok.
    public class Konut
    {
        public Guid Id { get; set; }

        // Sinir geometrisi, WKT metni olarak tutulur. Ornek: "POLYGON((32.86 39.95, ...))".
        public string Geometri { get; set; } = "";

        // Bu nesne hangi projeye ait (Proje.Id). FK yok - sunucu tarafinda kontrol edilir.
        public Guid ProjeId { get; set; }

        // UAVT adres kodu 10 haneli -> int'e sigmaz, long.
        public long UAVTKod { get; set; }
        public int BBKsayi { get; set; }
    }
}
