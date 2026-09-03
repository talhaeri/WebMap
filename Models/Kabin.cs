namespace WebMap.Models
{
    public class Kabin : NetworkElement
    {
        public string Kod { get; set; } = "";
        public string KabinTipi { get; set; } = "";
        public int KabinKapasitesi { get; set; }
        public int BosPort { get; set; }
    }
}
