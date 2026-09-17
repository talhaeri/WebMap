using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ProjeKilitliFiltresi: kilitli projeye yazma girisimini (ProjeKilitliException) 409 yanitina cevirir.
builder.Services.AddControllersWithViews(options => options.Filters.Add<ProjeKilitliFiltresi>());

// Proje kilidi (Services/ProjeKilidiInterceptor.cs). Durum tutmaz: tek ornek butun isteklerde kullanilir.
builder.Services.AddSingleton<ProjeKilidiInterceptor>();

//  veritabanı bağlantısı (MONSTER SQL Server -> WebMapDb) + her SaveChanges oncesi proje kilidi
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DbPath"))
    .AddInterceptors(sp.GetRequiredService<ProjeKilidiInterceptor>()));

// proje maliyet hesabi (Services/MaliyetHesaplayici.cs) - controller degil, ayri servis
builder.Services.AddScoped<MaliyetHesaplayici>();

// nesne yerlesim kurallari (Services/GeometriDenetimi.cs) - proje siniri + poligon ustu
builder.Services.AddScoped<GeometriDenetimi>();

builder.Services.AddScoped<PortDenetimi>();

// menhol / kabin / santral kodu butun projelerde tek (Services/KodDenetimi.cs)
builder.Services.AddScoped<KodDenetimi>();

// proje durum gecisleri: onaya gonder, geri cek, onayla, reddet, planlamaya al (Services/ProjeAkisi.cs)
builder.Services.AddScoped<ProjeAkisi>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<KullaniciBaglami>();

builder.Services.AddSingleton<IPasswordHasher<Kullanici>, PasswordHasher<Kullanici>>();

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

        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;

            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var rol = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var guncelRol = Guid.TryParse(id, out var g)
                ? await db.Kullanicilar.Where(k => k.Id == g).Select(k => k.Yetki).FirstOrDefaultAsync()
                : null;
            // Kullanici silinmis ya da rolu degismis: oturumu dusur.
            if (guncelRol != rol)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Program ilk calistiginda, bekleyen EF Core migration'larini veritabanina uygular.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Ilk yonetici: kullanici tablosu bossa olustur.
    // HasData kullanilmiyor, cunku parola ozeti her uretimde farkli cikar ve
    // her model kurulumunda yeni bir migration dogururdu.
    if (!db.Kullanicilar.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Kullanici>>();
        var yonetici = new Kullanici { KullaniciAdi = "admin", Yetki = Yetkiler.Yonetici };
        yonetici.ParolaHash = hasher.HashPassword(yonetici, "admin123");
        db.Kullanicilar.Add(yonetici);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MapStaticAssets uc nokta uretiyor; uc noktalar FallbackPolicy'ye takildigi icin
// CSS ve JS de giris istemeye baslar. Giris sayfasi kendi stilini yukleyemez.
app.MapStaticAssets().AllowAnonymous();

// API controller'lari ([Route("api/...")]) icin
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
