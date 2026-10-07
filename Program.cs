using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC. ProjeKilitliFiltresi: kilitli projeye yazma girişimini (ProjeKilitliException) 409 yanıtına çevirir.
builder.Services.AddControllersWithViews(options => options.Filters.Add<ProjeKilitliFiltresi>());

// Proje kilidi her SaveChanges'ten önce çalışır. Durum tutmaz, tek örnek yeter.
builder.Services.AddSingleton<ProjeKilidiInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DbPath"))
    .AddInterceptors(sp.GetRequiredService<ProjeKilidiInterceptor>()));

// İş kuralı servisleri (Services/)
builder.Services.AddScoped<MaliyetHesaplayici>();   // proje maliyeti
builder.Services.AddScoped<GeometriDenetimi>();     // proje sınırı ve poligon üstü kuralları
builder.Services.AddScoped<PortDenetimi>();         // boş port
builder.Services.AddScoped<KodDenetimi>();          // menhol, kabin, santral kodu tekliği
builder.Services.AddScoped<ProjeAkisi>();           // proje durum geçişleri

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KullaniciBaglami>();
builder.Services.AddSingleton<IPasswordHasher<Kullanici>, PasswordHasher<Kullanici>>();

// Giriş çerezini şifreleyen anahtarlar. Docker'da "AnahtarKlasoru" verilir (compose: /keys volume'u),
// böylece konteyner yenilense de oturumlar düşmez. Ayar boşsa ASP.NET varsayılanı kullanılır.
var anahtarKlasoru = builder.Configuration["AnahtarKlasoru"];
if (!string.IsNullOrWhiteSpace(anahtarKlasoru))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(anahtarKlasoru));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";
        options.LogoutPath = "/Hesap/Cikis";
        options.AccessDeniedPath = "/Hesap/Yetkisiz";
        options.ExpireTimeSpan = TimeSpan.FromHours(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events.OnRedirectToLogin = ApiIcinDurumKodu(StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = ApiIcinDurumKodu(StatusCodes.Status403Forbidden);

        // Her istekte kullanıcının rolü veritabanıyla karşılaştırılır
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var rol = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var guncelRol = Guid.TryParse(id, out var g)
                ? await db.Kullanicilar.Where(k => k.Id == g).Select(k => k.Yetki).FirstOrDefaultAsync()
                : null;
            // Kullanıcı silinmiş ya da rolü değişmiş: oturumu düşür
            if (guncelRol != rol)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // bekleyen migration'ları uygular

    // İlk yönetici: kullanıcı tablosu boşsa oluşturulur. HasData kullanılmaz: parola özeti her üretimde
    // farklı çıkar ve her model kurulumunda yeni bir migration doğururdu.
    if (!db.Kullanicilar.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Kullanici>>();
        var yonetici = new Kullanici { KullaniciAdi = "admin", Yetki = Yetkiler.Yonetici };
        yonetici.ParolaHash = hasher.HashPassword(yonetici, "admin123");
        db.Kullanicilar.Add(yonetici);
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MapStaticAssets uç nokta üretir; uç noktalar FallbackPolicy'ye takıldığı için CSS ve JS de giriş ister
// ve giriş sayfası kendi stilini yükleyemezdi. Bu yüzden anonim erişime açılır.
app.MapStaticAssets().AllowAnonymous();

// Sayfa controller'larının varsayılan rotası; [Route("api/...")] ile işaretli API controller'ları da bununla kaydolur
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// /api isteklerinde giriş sayfasına yönlendirmek yerine durum kodu döner, sayfalar yönlendirilir
static Func<RedirectContext<CookieAuthenticationOptions>, Task> ApiIcinDurumKodu(int durumKodu) => ctx =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
        ctx.Response.StatusCode = durumKodu;
    else
        ctx.Response.Redirect(ctx.RedirectUri);
    return Task.CompletedTask;
};
