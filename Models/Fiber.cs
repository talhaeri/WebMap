namespace WebMap.Models
{
    // Fiber guzergahi (LINESTRING WKT). Baslangic/bitis birer nesne referansidir;
    // FK constraint YOK - sunucu tarafinda kontrol edilir (bkz. FiberlerController).
    //   Baslangic: mutlaka bir Menhol, Kabin veya Santral.
    //   Bitis: bir NetworkElement (Menhol/Kabin), bir Konut ya da bir Santral.
    public class Fiber
    {
        public Guid Id { get; set; }

        // Ornek: "LINESTRING(32.86 39.95, 32.87 39.96)".
        public string Guzergah { get; set; } = "";

        // Guid referanslar: turden bagimsiz benzersiz olduklari icin artik ambiguity yok.
        public Guid BaslangicId { get; set; }
        public Guid BitisId { get; set; }

        // Bu nesne hangi projeye ait (Proje.Id). FK yok - sunucu tarafinda kontrol edilir.
        public Guid ProjeId { get; set; }
    }
}
