using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//  veritabanı bağlantısı (MONSTER SQL Server -> WebMapDb)
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DbPath")));

// proje maliyet hesabi (Services/MaliyetHesaplayici.cs) - controller degil, ayri servis
builder.Services.AddScoped<MaliyetHesaplayici>();

// nesne yerlesim kurallari (Services/GeometriDenetimi.cs) - proje siniri + poligon ustu
builder.Services.AddScoped<GeometriDenetimi>();

builder.Services.AddScoped<PortDenetimi>();

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
