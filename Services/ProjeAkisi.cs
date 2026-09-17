using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using static WebMap.Models.ProjeSabitler;

namespace WebMap.Services
{
    // Proje durum gecislerinin TEK yeri: OnayaGonder, GeriCek, Onayla, Reddet, PlanlamayaAl.
    // Kimin hangi durumda hangi gecisi yapabilecegine ProjeKurallari.Makine (Stateless) karar verir;
    // burasi kurali sorar, durumu degistirir, ilgili alanlari doldurur, gecmise yazar ve kaydeder.
    public class ProjeAkisi(AppDbContext db, KullaniciBaglami kullanici, MaliyetHesaplayici maliyet)
    {
        static readonly string[] NotGerekenler = [ProjeIslemleri.Reddet, ProjeIslemleri.PlanlamayaAl];

        // null donerse islem basarili; degilse kullaniciya gosterilecek hata metni.
        public async Task<string?> Uygula(Proje p, string islem, string? not)
        {
            var makine = ProjeKurallari.Makine(p, kullanici.Rol);
            if (!makine.CanFire(islem)) return "Bu işlem bu durumda ya da bu yetkiyle yapılamaz.";

            // Sadece bosluktan olusan not yok sayilir; kaydedilen not kirpilmis olur.
            not = string.IsNullOrWhiteSpace(not) ? null : not.Trim();
            if (NotGerekenler.Contains(islem) && not is null) return "Not zorunlu.";

            // Onaylayici yoksa proje sonsuza kadar bekler; yonetici de onaylayamaz.
            if (islem == ProjeIslemleri.OnayaGonder &&
                !await db.Kullanicilar.AnyAsync(k => k.Yetki == Yetkiler.Onaylayici))
                return "Sistemde onaylayıcı tanımlı değil.";

            makine.Fire(islem);   // p.Durum burada degisir
            string? maliyetJson = null;
            switch (islem)
            {
                case ProjeIslemleri.OnayaGonder: p.RedNotu = null; break;   // onceki turun red notu gecersiz
                case ProjeIslemleri.Reddet: p.RedNotu = not; break;          // duzenleyici Planlama'da gorecek
                case ProjeIslemleri.Onayla:
                    // Onay anindaki maliyet kopyasi: birim fiyatlar sonradan degisse de onayli rapor degismez.
                    maliyetJson = JsonSerializer.Serialize(await maliyet.Hesapla(p.Id), JsonSerializerOptions.Web);
                    (p.OnaylayanId, p.OnaylayanAdi, p.OnayTarihi, p.OnayMaliyeti) =
                        (kullanici.Id, kullanici.Ad, DateTime.UtcNow, maliyetJson);
                    break;
                case ProjeIslemleri.PlanlamayaAl:
                    // Guncel onay gecersiz; eski onay ve maliyet kopyasi gecmisteki Onayla satirinda duruyor.
                    (p.OnaylayanId, p.OnaylayanAdi, p.OnayTarihi, p.OnayMaliyeti) = (null, null, null, null);
                    break;
            }

            // Durum degisikligi ve gecmis satiri tek SaveChanges: ya ikisi birden yazilir ya hicbiri.
            db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(p.Id, p.ProjeAdi, islem, kullanici.Id, kullanici.Ad, not, maliyetJson));
            await db.SaveChangesAsync();
            return null;
        }
    }
}
