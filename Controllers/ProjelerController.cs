using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;
using static WebMap.Models.ProjeSabitler;

namespace WebMap.Controllers;

[ApiController]
[Route("api/projeler")]
public class ProjelerController(AppDbContext db, MaliyetHesaplayici maliyetHesaplayici, ProjeAkisi akis, KullaniciBaglami kullanici) : ControllerBase
{
    // Onay bekleyenler listenin basinda, sonra ada gore.
    // Goruntuleme rolu query filter sayesinde sadece onayli projeleri alir.
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Projeler
            .OrderByDescending(p => p.Durum == ProjeDurumlari.OnayBekliyor)
            .ThenBy(p => p.ProjeAdi)
            .Select(p => new { p.Id, p.ProjeAdi, p.Geometri, OlusturmaTarihi = Utc(p.OlusturmaTarihi), p.Durum })
            .ToListAsync();
        return Ok(liste);
    }

    // Tek proje ve oturumdaki kullanicinin bu projede neler yapabilecegi (izinler).
    // Arayuz butonlari bu listeden uretilecek; kural JavaScript'e kopyalanmaz.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detay(Guid id)
    {
        var proje = await db.Projeler.FindAsync(id);   // query filter uygulanir: gorunmeyen proje 404
        if (proje is null) return NotFound();

        return Ok(new
        {
            proje.Id,
            proje.ProjeAdi,
            proje.Geometri,
            proje.Durum,
            OlusturmaTarihi = Utc(proje.OlusturmaTarihi),
            proje.OlusturanAdi,
            proje.OnaylayanAdi,
            OnayTarihi = Utc(proje.OnayTarihi),
            proje.RedNotu,
            Izinler = new
            {
                Islemler = (await ProjeKurallari.Makine(proje, kullanici.Rol).PermittedTriggersAsync).ToList(),
                NesneDuzenleyebilir = ProjeKurallari.NesneDuzenleyebilir(kullanici.Rol, proje.Durum),
                Silebilir = ProjeKurallari.ProjeSilebilir(kullanici.Rol)
            }
        });
    }

    // Govde: { "projeAdi": "...", "geometri": "POLYGON((...))" }. Yeni proje her zaman Planlama'da baslar.
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] ProjeEkleDto dto)
    {
        var proje = new Proje
        {
            ProjeAdi = dto.ProjeAdi,
            Geometri = dto.Geometri,
            OlusturanId = kullanici.Id,
            OlusturanAdi = kullanici.Ad
        };
        db.Projeler.Add(proje);   // Guid Id burada uretilir; gecmis satiri onu kullanir
        db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(proje.Id, proje.ProjeAdi, ProjeIslemleri.Olustur, kullanici.Id, kullanici.Ad));
        await db.SaveChangesAsync();
        return Ok(new { proje.Id, proje.ProjeAdi, proje.Geometri, OlusturmaTarihi = Utc(proje.OlusturmaTarihi), proje.Durum });
    }

    // Durum gecisi. Govde: { "islem": "OnayaGonder" | "GeriCek" | "Onayla" | "Reddet" | "PlanlamayaAl", "not": "..." }
    // Rol ozniteligi yok: kimin hangi durumda ne yapabilecegini ProjeKurallari.Makine belirliyor.
    [HttpPost("{id:guid}/islem")]
    public async Task<IActionResult> Islem(Guid id, [FromBody] ProjeIslemDto dto)
    {
        var proje = await db.Projeler.FindAsync(id);
        if (proje is null) return NotFound();

        var hata = await akis.Uygula(proje, dto.Islem, dto.Not);
        return hata is null ? Ok(new { proje.Id, proje.Durum }) : BadRequest(hata);
    }

    // Onayli projede ONAY ANINDAKI kopya (birim fiyatlar sonradan degisse de rapor degismez),
    // diger durumlarda anlik hesap. Kalem ve toplam alanlari eskisiyle ayni (map.js degismeden calisir);
    // kaynak / onaylayanAdi / onayTarihi yeni eklenen alanlar.
    [HttpGet("{id:guid}/maliyet")]
    public async Task<IActionResult> Maliyet(Guid id)
    {
        var proje = await db.Projeler.FindAsync(id);
        if (proje is null) return NotFound();

        var onayKopyasi = proje.Durum == ProjeDurumlari.Onaylandi && proje.OnayMaliyeti is not null;
        var m = onayKopyasi
            ? JsonSerializer.Deserialize<MaliyetSonucu>(proje.OnayMaliyeti!, JsonSerializerOptions.Web)!
            : await maliyetHesaplayici.Hesapla(id);

        return Ok(new
        {
            Kaynak = onayKopyasi ? "onay" : "guncel",
            proje.OnaylayanAdi,
            OnayTarihi = Utc(proje.OnayTarihi),
            m.Kalemler,
            m.IscilikGenelToplam,
            m.MalzemeGenelToplam,
            m.GenelToplam
        });
    }

    // Projenin olay gecmisi, yeniden eskiye. Maliyet kopyasi (MaliyetJson) listeye konmaz.
    [HttpGet("{id:guid}/gecmis")]
    public async Task<IActionResult> Gecmis(Guid id)
    {
        if (!await db.Projeler.AnyAsync(p => p.Id == id)) return NotFound();

        var liste = await db.ProjeGecmisi
            .Where(g => g.ProjeId == id)
            .OrderByDescending(g => g.Tarih)
            .Select(g => new { Tarih = Utc(g.Tarih), g.Islem, g.KullaniciAdi, g.Not })
            .ToListAsync();
        return Ok(liste);
    }

    // Proje silme sadece yoneticide (her durumda). ProjeKilidiInterceptor ayni kayitta silinen
    // projenin nesnelerini kilide takmaz; bu yuzden bu ucun yoneticiye kapali kalmasi sart.
    // Gecmis FK'siz oldugu icin proje silindikten sonra da kalir.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Yonetici)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var proje = await db.Projeler.FindAsync(id);
        if (proje is null) return NotFound();

        // Projeye bagli tum nesneler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.ProjeId == id));
        db.Menholler.RemoveRange(db.Menholler.Where(m => m.ProjeId == id));
        db.Kabinler.RemoveRange(db.Kabinler.Where(k => k.ProjeId == id));
        db.Santraller.RemoveRange(db.Santraller.Where(s => s.ProjeId == id));
        db.Konutlar.RemoveRange(db.Konutlar.Where(k => k.ProjeId == id));

        db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(proje.Id, proje.ProjeAdi, ProjeIslemleri.Sil, kullanici.Id, kullanici.Ad));
        db.Projeler.Remove(proje);
        await db.SaveChangesAsync();
        return Ok();
    }

    // Tarihler veritabanina UTC yaziliyor ama okunurken "saat dilimi belirsiz" geliyor ve JSON'a
    // "Z" olmadan yaziliyor; tarayici bunu yerel saat sanip 3 saat kaydirir. UTC olarak isaretlenir.
    static DateTime? Utc(DateTime? t) => t is null ? null : DateTime.SpecifyKind(t.Value, DateTimeKind.Utc);
}

public record ProjeEkleDto(
    [Required(ErrorMessage = "Proje adı zorunlu.")]
    [StringLength(100, ErrorMessage = "Proje adı en fazla 100 karakter.")]
    string ProjeAdi,

    [Required(ErrorMessage = "Geometri zorunlu.")]
    string Geometri);

public record ProjeIslemDto(
    [Required(ErrorMessage = "İşlem zorunlu.")]
    string Islem,

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter.")]
    string? Not);
