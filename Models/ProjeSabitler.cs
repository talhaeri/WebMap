namespace WebMap.Models
{
    public class ProjeSabitler
    {
        public static class ProjeDurumlari
        {
            public const string Planlama = "Planlama", OnayBekliyor = "OnayBekliyor", Onaylandi = "Onaylandi";
        }
        // Hem Stateless tetikleyicisi hem gecmis kaydindaki islem adi.
        public static class ProjeIslemleri
        {
            public const string Olustur = "Olustur", OnayaGonder = "OnayaGonder", GeriCek = "GeriCek",
                Onayla = "Onayla", Reddet = "Reddet", PlanlamayaAl = "PlanlamayaAl",
                Degistir = "Degistir", Sil = "Sil";
        }
    }
}
