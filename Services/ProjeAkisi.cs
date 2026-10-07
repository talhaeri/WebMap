using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Services
{
    // Proje durum geçişlerinin tek yeri: OnayaGonder, GeriCek, Onayla, Reddet, PlanlamayaAl.
    // Geçişe izin verip vermeyeceğine ProjeKurallari.Makine karar verir; burası durumu değiştirir,
    // ilgili alanları doldurur, geçmişe yazar ve kaydeder.
    public class ProjeAkisi(AppDbContext db, KullaniciBaglami kullanici, MaliyetHesaplayici maliyet)
    {
        static readonly string[] NotGerekenler = [ProjeIslemleri.Reddet, ProjeIslemleri.PlanlamayaAl];

        // Başarılıysa null döner, değilse kullanıcıya gösterilecek hata metni
        public async Task<string?> Uygula(Proje p, string islem, string? not)
        {
            var makine = ProjeKurallari.Makine(p, kullanici.Rol);
            if (!makine.CanFire(islem)) return "Bu işlem bu durumda ya da bu yetkiyle yapılamaz.";

            // Sadece boşluktan oluşan not yok sayılır; kaydedilen not kırpılmış olur
            not = string.IsNullOrWhiteSpace(not) ? null : not.Trim();
            if (NotGerekenler.Contains(islem) && not is null) return "Not zorunlu.";

            // Onaylayıcı yoksa proje sonsuza kadar bekler
            if (islem == ProjeIslemleri.OnayaGonder &&
                !await db.Kullanicilar.AnyAsync(k => k.Yetki == Yetkiler.Onaylayici))
                return "Sistemde onaylayıcı tanımlı değil.";

            makine.Fire(islem);   // p.Durum burada değişir
            string? maliyetJson = null;
            switch (islem)
            {
                case ProjeIslemleri.OnayaGonder: p.RedNotu = null; break;   // önceki turun red notu geçersiz
                case ProjeIslemleri.Reddet: p.RedNotu = not; break;         // düzenleyici Planlama'da görür
                case ProjeIslemleri.Onayla:
                    // Onay anındaki maliyet kopyası: birim fiyatlar sonradan değişse de onaylı rapor değişmez
                    maliyetJson = JsonSerializer.Serialize(await maliyet.Hesapla(p.Id), JsonSerializerOptions.Web);
                    (p.OnaylayanId, p.OnaylayanAdi, p.OnayTarihi, p.OnayMaliyeti) =
                        (kullanici.Id, kullanici.Ad, DateTime.UtcNow, maliyetJson);
                    break;
                case ProjeIslemleri.PlanlamayaAl:
                    // Geçerli onay kalkar; eski onay ve maliyet kopyası geçmişteki Onayla satırında durur
                    (p.OnaylayanId, p.OnaylayanAdi, p.OnayTarihi, p.OnayMaliyeti) = (null, null, null, null);
                    break;
            }

            // Durum ve geçmiş satırı tek SaveChanges'te: ya ikisi birden yazılır ya hiçbiri
            db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(p.Id, p.ProjeAdi, islem, kullanici.Id, kullanici.Ad, not, maliyetJson));
            await db.SaveChangesAsync();
            return null;
        }
    }
}
