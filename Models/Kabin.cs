namespace WebMap.Models
{
    public class Kabin : NetworkElement
    {
        public string Kod { get; set; } = "";
        public string KabinTipi { get; set; } = "";   // MDU, Splitter ya da OLT
        public int KabinKapasitesi { get; set; }
    }
}
