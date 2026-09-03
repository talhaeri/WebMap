namespace WebMap.Models
{
    // Ticari amacli bina (poligon). Konut ile ayni mantik. TPT: "Ticariler" tablosu, Id -> Alanlar.Id FK.
    public class Ticari : Alan
    {
        public int UAVTKod { get; set; }
        public int IsyeriSayisi { get; set; }
    }
}
