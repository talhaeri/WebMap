namespace WebMap.Models
{
    // Fiber guzergahi (LINESTRING WKT). Baslangic/bitis birer nesne referansidir;
    // FK constraint YOK - sunucu tarafinda kontrol edilir (bkz. FiberlerController).
    //   Baslangic: mutlaka bir Menhol veya Kabin.
    //   Bitis: bir NetworkElement (Menhol/Kabin) ya da bir Alan (Alan/Konut/Ticari).
    public class Fiber
    {
        public int Id { get; set; }

        // Ornek: "LINESTRING(32.86 39.95, 32.87 39.96)".
        public string Guzergah { get; set; } = "";

        public int BaslangicId { get; set; }
        public int BitisId { get; set; }
    }
}
