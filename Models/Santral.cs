namespace WebMap.Models
{
    // Santral: alan (poligon), Konut gibi bagimsiz. NetworkElement DEGIL.
    // Sinir geometrisi WKT metni olarak tutulur. Ornek: "POLYGON((32.86 39.95, ...))".
    public class Santral : IProjeyeAit
    {
        public Guid Id { get; set; }

        public string Geometri { get; set; } = "";

        // Bu nesne hangi projeye ait (Proje.Id). FK yok - sunucu tarafinda kontrol edilir.
        public Guid ProjeId { get; set; }

        public string Kod { get; set; } = "";
        public int Kapasite { get; set; }
    }
}
