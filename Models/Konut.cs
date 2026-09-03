namespace WebMap.Models
{
    // Konut amacli bina (poligon). TPT: "Konutlar" tablosu, Id -> Alanlar.Id FK.
    public class Konut : Alan
    {
        public int UAVTKod { get; set; }
        public int BBKsayi { get; set; }
    }
}
