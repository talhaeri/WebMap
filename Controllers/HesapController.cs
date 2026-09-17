using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers
{
    // Program.cs'teki FallbackPolicy her seyi kapatiyor; giris sayfasinin acik kalmasi icin bu controller acikca [AllowAnonymous].
    [AllowAnonymous]
    public class HesapController(AppDbContext db, IPasswordHasher<Kullanici> hasher) : Controller
    {
        [HttpGet]
        public IActionResult Giris(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Giris(string kullaniciAdi, string parola, string? returnUrl = null)
        {
            var kullanici = await db.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi);

            var gecerli = kullanici is not null &&
                hasher.VerifyHashedPassword(kullanici, kullanici.ParolaHash, parola)
                    != PasswordVerificationResult.Failed;

            // Kullanici yoksa da parola yanlissa da AYNI mesaj: hangi kullanici adinin var oldugu disariya sizmasin.
            if (!gecerli)
            {
                ViewData["Hata"] = "Kullanici adi veya parola hatali.";
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // Yetki cereze rol talebi olarak yaziliyor; [Authorize(Roles = ...)] bunu okuyor.
            var talepler = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, kullanici!.Id.ToString()),
                new(ClaimTypes.Name, kullanici.KullaniciAdi),
                new(ClaimTypes.Role, kullanici.Yetki)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(talepler, CookieAuthenticationDefaults.AuthenticationScheme)));

            // IsLocalUrl: baska siteye yonlendirme acigini kapatir.
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Giris));
        }

        public IActionResult Yetkisiz() => View();
    }
}
