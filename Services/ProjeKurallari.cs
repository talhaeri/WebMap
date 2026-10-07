using Stateless;
using WebMap.Models;

namespace WebMap.Services
{
    // Proje izinlerinin tek kaynağı: hangi rol, hangi durumda ne yapabilir.
    // Arayüz bu kuralları sunucudan okur (GET /api/projeler/{id} -> izinler), kopyalamaz.
    public static class ProjeKurallari
    {
        static bool Duzenleyici(string? rol) => rol is Yetkiler.Yonetici or Yetkiler.Duzenleme;

        // Bilinmeyen ya da boş rol de en kısıtlı davranışı alır
        public static bool SadeceOnaylilariGorur(string? rol) => !Duzenleyici(rol) && rol != Yetkiler.Onaylayici;
        public static bool ProjeOlusturabilir(string? rol) => Duzenleyici(rol);
        public static bool ProjeSilebilir(string? rol) => rol == Yetkiler.Yonetici;
        public static bool ProjeYazdirabilir(string? rol, string durum) => Duzenleyici(rol) || durum == ProjeDurumlari.Onaylandi;

        public static bool NesneDuzenleyebilir(string? rol, string durum) => durum switch
        {
            ProjeDurumlari.Planlama => Duzenleyici(rol),
            ProjeDurumlari.OnayBekliyor => rol == Yetkiler.Yonetici,
            _ => false   // Onaylandı: önce PlanlamayaAl
        };

        // Durum geçişleri. Makine proje.Durum alanını doğrudan okuyup yazar.
        public static StateMachine<string, string> Makine(Proje p, string? rol)
        {
            var m = new StateMachine<string, string>(() => p.Durum, d => p.Durum = d);
            m.Configure(ProjeDurumlari.Planlama)
                .PermitIf(ProjeIslemleri.OnayaGonder, ProjeDurumlari.OnayBekliyor, () => Duzenleyici(rol));
            m.Configure(ProjeDurumlari.OnayBekliyor)
                .PermitIf(ProjeIslemleri.GeriCek, ProjeDurumlari.Planlama, () => Duzenleyici(rol))
                .PermitIf(ProjeIslemleri.Onayla, ProjeDurumlari.Onaylandi, () => rol == Yetkiler.Onaylayici)
                .PermitIf(ProjeIslemleri.Reddet, ProjeDurumlari.Planlama, () => rol == Yetkiler.Onaylayici);
            m.Configure(ProjeDurumlari.Onaylandi)
                .PermitIf(ProjeIslemleri.PlanlamayaAl, ProjeDurumlari.Planlama, () => rol == Yetkiler.Yonetici);
            m.Configure(ProjeDurumlari.Planlama)
                .PermitIf(ProjeIslemleri.Yazdir, ProjeDurumlari.Onaylandi, () => rol == Yetkiler.Goruntuleme, ProjeDurumlari.Planlama);
            return m;
        }
    }
}
