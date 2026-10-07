namespace WebMap.Models
{
    // Santral: poligon alan. Konut gibi bağımsızdır, NetworkElement değildir.
    public class Santral : IProjeyeAit
    {
        public Guid Id { get; set; }

        // WKT poligon. Örnek: "POLYGON((32.86 39.95, ...))"
        public string Geometri { get; set; } = "";

        public Guid ProjeId { get; set; }

        public string Kod { get; set; } = "";
        public int Kapasite { get; set; }
    }
}
