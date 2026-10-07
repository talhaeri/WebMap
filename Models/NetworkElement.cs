namespace WebMap.Models
{
    // Ağdaki nokta tipli elemanların (Menhol, Kabin) ortak tabanı.
    // TPT: bu sınıf NetworkElements tablosuna, alt tipler kendi tablolarına yazılır.
    // abstract: doğrudan örneği oluşturulamaz, taban tabloda başıboş satır kalmaz.
    public abstract class NetworkElement : IProjeyeAit
    {
        public Guid Id { get; set; }

        // WKT nokta. Örnek: "POINT(32.866 39.959)"
        public string Konum { get; set; } = "";

        public Guid ProjeId { get; set; }
    }
}
