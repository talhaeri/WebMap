using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers
{
    // Kullanici yonetimi yalnizca yoneticide.
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
            else if (yetki != Yetkiler.Yonetici && yetki != Yetkiler.Duzenleme && yetki != Yetkiler.Goruntuleme)
                TempData["Hata"] = "Gecersiz yetki.";
            else
            {
                var kullanici = new Kullanici { KullaniciAdi = yeniKullaniciAdi.Trim(), Yetki = yetki };
                // Acik parola hicbir yerde saklanmiyor, sadece ozeti yaziliyor.
                kullanici.ParolaHash = hasher.HashPassword(kullanici, yeniParola);
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

            // Son yonetici silinirse sisteme bir daha kimse giremez.
            if (kullanici.Yetki == Yetkiler.Yonetici &&
                await db.Kullanicilar.CountAsync(k => k.Yetki == Yetkiler.Yonetici) == 1)
            {
                TempData["Hata"] = "Son yonetici silinemez.";
                return RedirectToAction(nameof(Index));
            }

            db.Kullanicilar.Remove(kullanici);
            await db.SaveChangesAsync();
            TempData["Bilgi"] = $"\"{kullanici.KullaniciAdi}\" silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
