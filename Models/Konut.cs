namespace WebMap.Models
{
    // Konut amaçlı bina (poligon)
    public class Konut : IProjeyeAit
    {
        public Guid Id { get; set; }

        // WKT poligon. Örnek: "POLYGON((32.86 39.95, ...))"
        public string Geometri { get; set; } = "";

        public Guid ProjeId { get; set; }

        public long UAVTKod { get; set; }   // 10 haneli adres kodu, int'e sığmaz
        public int BBKsayi { get; set; }
    }
}
