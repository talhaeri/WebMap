using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/kabinler")]
public class KabinlerController(AppDbContext db, GeometriDenetimi denetim, PortDenetimi port, KodDenetimi kodDenetimi) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] Guid projeId)
    {
        var bos = await port.BosPortlar(projeId);
        var liste = await db.Kabinler
            .Where(k => k.ProjeId == projeId)
            .Select(k => new { k.Id, k.Konum, k.Kod, k.KabinTipi, k.KabinKapasitesi })
            .ToListAsync();
        return Ok(liste.Select(k => new { k.Id, k.Konum, k.Kod, k.KabinTipi, k.KabinKapasitesi, BosPort = bos.GetValueOrDefault(k.Id) }));
    }

    // Govde: { "projeId": "<guid>", "konum": "POINT(lon lat)", "kod": "...", "kabinTipi": "...", "kabinKapasitesi": 288 }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] KabinEkleDto dto)
    {
        // Proje siniri + poligon ustune yerlesim kurallari (Services/GeometriDenetimi.cs)
        var hata = await denetim.Denetle(dto.ProjeId, dto.Konum, "Kabin", dto.KabinTipi);
        if (hata is not null) return BadRequest(hata);

        // Kod butun projelerde tek olmali (Services/KodDenetimi.cs)
        var kodHatasi = await kodDenetimi.Denetle("Kabin", dto.Kod);
        if (kodHatasi is not null) return BadRequest(kodHatasi);

        var kabin = new Kabin
        {
            ProjeId = dto.ProjeId,
            Konum = dto.Konum,
            Kod = dto.Kod,
            KabinTipi = dto.KabinTipi,
            KabinKapasitesi = dto.KabinKapasitesi
        };
        db.Kabinler.Add(kabin);
        await db.SaveChangesAsync();
        return Ok(new { kabin.Id, kabin.Konum, kabin.Kod, kabin.KabinTipi, kabin.KabinKapasitesi, BosPort = kabin.KabinKapasitesi });
    }

    // Govde: Ekle ile ayni sekil; ProjeId ve Konum degistirilemez (goz ardi edilir).
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Guncelle(Guid id, [FromBody] KabinEkleDto dto)
    {
        var kabin = await db.Kabinler.FindAsync(id);
        if (kabin is null) return NotFound();

        // Kod butun projelerde tek olmali (Services/KodDenetimi.cs)
        var kodHatasi = await kodDenetimi.Denetle("Kabin", dto.Kod, id);
        if (kodHatasi is not null) return BadRequest(kodHatasi);

        // Kapasite bagli fiber sayisinin altina indirilemez (bos port eksiye duserdi)
        var kullanilan = await port.FiberSayisi(id);
        if (dto.KabinKapasitesi < kullanilan)
            return BadRequest($"Kapasite bagli fiber sayisindan ({kullanilan}) kucuk olamaz.");

        var hata = await denetim.Denetle(kabin.ProjeId, kabin.Konum, "Kabin", dto.KabinTipi);
        if (hata is not null) return BadRequest(hata);

        kabin.Kod = dto.Kod;
        kabin.KabinTipi = dto.KabinTipi;
        kabin.KabinKapasitesi = dto.KabinKapasitesi;
        await db.SaveChangesAsync();
        return Ok(new { kabin.Id, kabin.Konum, kabin.Kod, kabin.KabinTipi, kabin.KabinKapasitesi, BosPort = kabin.KabinKapasitesi - kullanilan });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var kabin = await db.Kabinler.FindAsync(id);
        if (kabin is null) return NotFound();
        // Bu nesneye bagli fiberler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.BaslangicId == id || f.BitisId == id));
        db.Kabinler.Remove(kabin);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record KabinEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Konum zorunlu.")]
    string Konum,

    [Required(ErrorMessage = "Kod zorunlu.")]
    [RegularExpression(@"^KBN-[A-Z0-9]{7}$", ErrorMessage = "Kod 'KBN-' + 7 buyuk harf/rakam olmali.")]
    string Kod,

    [Required(ErrorMessage = "Tip zorunlu.")]
    [StringLength(50, ErrorMessage = "Tip en fazla 50 karakter.")]
    string KabinTipi,

    [Range(1, 100000, ErrorMessage = "Kapasite 1-100000 arasi olmali.")]
    int KabinKapasitesi);
