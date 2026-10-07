using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers
{
    // Kullanıcı yönetimi yalnızca yöneticide
    [Authorize(Roles = Yetkiler.Yonetici)]
    public class KullanicilarController(AppDbContext db, IPasswordHasher<Kullanici> hasher) : Controller
    {
        public async Task<IActionResult> Index() =>
            View(await db.Kullanicilar.OrderBy(k => k.KullaniciAdi).ToListAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(string yeniKullaniciAdi, string yeniParola, string yetki)
        {
            if (string.IsNullOrWhiteSpace(yeniKullaniciAdi) || string.IsNullOrWhiteSpace(yeniParola))
                TempData["Hata"] = "Kullanici adi ve parola zorunlu.";
            else if (await db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == yeniKullaniciAdi))
                TempData["Hata"] = "Bu kullanici adi zaten var.";
            else if (!Yetkiler.TumYetkiler.Contains(yetki))
                TempData["Hata"] = "Gecersiz yetki.";
            else
            {
                var kullanici = new Kullanici { KullaniciAdi = yeniKullaniciAdi.Trim(), Yetki = yetki };
                kullanici.ParolaHash = hasher.HashPassword(kullanici, yeniParola);   // açık parola saklanmaz, sadece özeti
                db.Kullanicilar.Add(kullanici);
                await db.SaveChangesAsync();
                TempData["Bilgi"] = $"\"{kullanici.KullaniciAdi}\" eklendi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(Guid id)
        {
            var kullanici = await db.Kullanicilar.FindAsync(id);
            if (kullanici is null) return RedirectToAction(nameof(Index));

            var neden = await SilinemezNedeni(kullanici);
            if (neden is not null)
            {
                TempData["Hata"] = neden;
                return RedirectToAction(nameof(Index));
            }

            db.Kullanicilar.Remove(kullanici);
            await db.SaveChangesAsync();
            TempData["Bilgi"] = $"\"{kullanici.KullaniciAdi}\" silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Silinemiyorsa nedenini, silinebiliyorsa null döner
        async Task<string?> SilinemezNedeni(Kullanici k)
        {
            // Son yönetici silinirse sisteme bir daha kimse giremez
            if (k.Yetki == Yetkiler.Yonetici && await db.Kullanicilar.CountAsync(x => x.Yetki == Yetkiler.Yonetici) == 1)
                return "Son yonetici silinemez.";

            // Son onaylayıcı silinirse bekleyen projeler onaylanamaz
            if (k.Yetki == Yetkiler.Onaylayici && await db.Kullanicilar.CountAsync(x => x.Yetki == Yetkiler.Onaylayici) == 1)
            {
                // Filtre atlanır: kural sayfayı kimin açtığına değil projelerin gerçek durumuna bakmalı
                var bekleyen = await db.Projeler.IgnoreQueryFilters()
                    .CountAsync(p => p.Durum == ProjeDurumlari.OnayBekliyor);
                if (bekleyen > 0)
                    return $"{bekleyen} proje onay bekliyor; son onaylayıcı silinemez. Önce yeni bir onaylayıcı ekleyin.";
            }
            return null;
        }
    }
}
