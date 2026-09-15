using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/konutlar")]
public class KonutlarController(AppDbContext db, GeometriDenetimi denetim) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] Guid projeId)
    {
        var liste = await db.Konutlar
            .Where(k => k.ProjeId == projeId)
            .Select(k => new { k.Id, k.Geometri, k.UAVTKod, k.BBKsayi })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "projeId": "<guid>", "geometri": "POLYGON((...))", "uavtKod": 123, "bbKsayi": 8 }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] KonutEkleDto dto)
    {
        // Proje siniri + poligon ustune yerlesim kurallari (Services/GeometriDenetimi.cs)
        var hata = await denetim.Denetle(dto.ProjeId, dto.Geometri, "Konut");
        if (hata is not null) return BadRequest(hata);

        var konut = new Konut { ProjeId = dto.ProjeId, Geometri = dto.Geometri, UAVTKod = dto.UAVTKod, BBKsayi = dto.BBKsayi };
        db.Konutlar.Add(konut);
        await db.SaveChangesAsync();
        return Ok(new { konut.Id, konut.Geometri, konut.UAVTKod, konut.BBKsayi });
    }

    // Govde: Ekle ile ayni sekil; ProjeId ve Geometri degistirilemez (goz ardi edilir).
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Guncelle(Guid id, [FromBody] KonutEkleDto dto)
    {
        var konut = await db.Konutlar.FindAsync(id);
        if (konut is null) return NotFound();

        konut.UAVTKod = dto.UAVTKod;
        konut.BBKsayi = dto.BBKsayi;
        await db.SaveChangesAsync();
        return Ok(new { konut.Id, konut.Geometri, konut.UAVTKod, konut.BBKsayi });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var konut = await db.Konutlar.FindAsync(id);
        if (konut is null) return NotFound();
        // Bu nesneye bagli fiberler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.BaslangicId == id || f.BitisId == id));
        db.Konutlar.Remove(konut);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record KonutEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Geometri zorunlu.")]
    string Geometri,

    [Range(1000000000, 9999999999, ErrorMessage = "UAVT Kod 10 haneli bir sayi olmali.")]
    long UAVTKod,

    [Range(0, 100000, ErrorMessage = "BBK sayisi 0-100000 arasi olmali.")]
    int BBKsayi);
