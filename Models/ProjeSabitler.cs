namespace WebMap.Models
{
    public static class ProjeDurumlari
    {
        public const string Planlama = "Planlama", OnayBekliyor = "OnayBekliyor", Onaylandi = "Onaylandi";
    }

    // Hem Stateless tetikleyicisi hem geçmiş kaydındaki işlem adı
    public static class ProjeIslemleri
    {
        public const string Olustur = "Olustur", OnayaGonder = "OnayaGonder", GeriCek = "GeriCek", Yazdir = "Yazdir",
            Onayla = "Onayla", Reddet = "Reddet", PlanlamayaAl = "PlanlamayaAl",
            Degistir = "Degistir", Sil = "Sil";
    }
}
