using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/santraller")]
public class SantrallerController(AppDbContext db, GeometriDenetimi denetim, PortDenetimi port) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] Guid projeId)
    {
        var bos = await port.BosPortlar(projeId);
        var liste = await db.Santraller
            .Where(s => s.ProjeId == projeId)
            .Select(s => new { s.Id, s.Geometri, s.Kod, s.Kapasite })
            .ToListAsync();
        return Ok(liste.Select(s => new { s.Id, s.Geometri, s.Kod, s.Kapasite, BosPort = bos.GetValueOrDefault(s.Id) }));
    }

    // Govde: { "projeId": "<guid>", "geometri": "POLYGON((...))", "kod": "...", "kapasite": 1000 }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] SantralEkleDto dto)
    {
        // Proje siniri + poligon ustune yerlesim kurallari (Services/GeometriDenetimi.cs)
        var hata = await denetim.Denetle(dto.ProjeId, dto.Geometri, "Santral");
        if (hata is not null) return BadRequest(hata);

        var santral = new Santral
        {
            ProjeId = dto.ProjeId,
            Geometri = dto.Geometri,
            Kod = dto.Kod,
            Kapasite = dto.Kapasite
        };
        db.Santraller.Add(santral);
        await db.SaveChangesAsync();
        // Yeni santralin hic fiberi yok: bos port = kapasite
        return Ok(new { santral.Id, santral.Geometri, santral.Kod, santral.Kapasite, BosPort = santral.Kapasite });
    }

    // Govde: Ekle ile ayni sekil; ProjeId ve Geometri degistirilemez (goz ardi edilir).
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Guncelle(Guid id, [FromBody] SantralEkleDto dto)
    {
        var santral = await db.Santraller.FindAsync(id);
        if (santral is null) return NotFound();

        // Kapasite bagli fiber sayisinin altina indirilemez (bos port eksiye duserdi)
        var kullanilan = await port.FiberSayisi(id);
        if (dto.Kapasite < kullanilan)
            return BadRequest($"Kapasite bagli fiber sayisindan ({kullanilan}) kucuk olamaz.");

        santral.Kod = dto.Kod;
        santral.Kapasite = dto.Kapasite;
        await db.SaveChangesAsync();
        return Ok(new { santral.Id, santral.Geometri, santral.Kod, santral.Kapasite, BosPort = santral.Kapasite - kullanilan });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var santral = await db.Santraller.FindAsync(id);
        if (santral is null) return NotFound();
        // Bu nesneye bagli fiberler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.BaslangicId == id || f.BitisId == id));
        db.Santraller.Remove(santral);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record SantralEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Geometri zorunlu.")]
    string Geometri,

    [Required(ErrorMessage = "Kod zorunlu.")]
    [RegularExpression(@"^SNTR-[A-Z0-9]{7}$", ErrorMessage = "Kod 'SNTR-' + 7 buyuk harf/rakam olmali.")]
    string Kod,

    [Range(1, 100000, ErrorMessage = "Kapasite 1-100000 arasi olmali.")]
    int Kapasite);
