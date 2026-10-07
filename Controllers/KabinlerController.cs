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

    // Gövde: { "projeId": "<guid>", "konum": "POINT(lon lat)", "kod": "...", "kabinTipi": "...", "kabinKapasitesi": 8 }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] KabinEkleDto dto)
    {
        var hata = await denetim.Denetle(dto.ProjeId, dto.Konum, "Kabin", dto.KabinTipi);   // yerleşim kuralları
        if (hata is not null) return BadRequest(hata);

        var kodHatasi = await kodDenetimi.Denetle("Kabin", dto.Kod);   // kod bütün projelerde tek
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

    // Gövde Ekle ile aynı; ProjeId ve Konum değiştirilemez (yok sayılır)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Guncelle(Guid id, [FromBody] KabinEkleDto dto)
    {
        var kabin = await db.Kabinler.FindAsync(id);
        if (kabin is null) return NotFound();

        var kodHatasi = await kodDenetimi.Denetle("Kabin", dto.Kod, id);
        if (kodHatasi is not null) return BadRequest(kodHatasi);

        // Kapasite bağlı fiber sayısının altına inemez (boş port eksiye düşerdi)
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
        db.Fiberler.RemoveRange(db.BagliFiberler(id));   // bağlı fiberler de silinir (FK yok)
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
