namespace WebMap.Models
{
    // Fiber güzergahı. Başlangıç ve bitiş, nesne Id'sidir; FK yok, FiberlerController denetler.
    //   Başlangıç: menhol, kabin ya da santral.
    //   Bitiş: menhol, kabin, konut ya da santral.
    // Id'ler tür fark etmeksizin benzersiz (Guid) olduğu için uç hangi tabloda olursa olsun ayırt edilir.
    public class Fiber : IProjeyeAit
    {
        public Guid Id { get; set; }

        // WKT çizgi. Örnek: "LINESTRING(32.86 39.95, 32.87 39.96)"
        public string Guzergah { get; set; } = "";

        public Guid BaslangicId { get; set; }
        public Guid BitisId { get; set; }

        public Guid ProjeId { get; set; }
    }
}
