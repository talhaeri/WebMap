using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/menholler")]
public class MenhollerController(AppDbContext db, GeometriDenetimi denetim, KodDenetimi kodDenetimi) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] Guid projeId)
    {
        var liste = await db.Menholler
            .Where(m => m.ProjeId == projeId)
            .Select(m => new { m.Id, m.Konum, m.Kod, m.Derinlik })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "projeId": "<guid>", "konum": "POINT(lon lat)", "kod": "...", "derinlik": 1.5 }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] MenholEkleDto dto)
    {
        // Proje siniri + poligon ustune yerlesim kurallari (Services/GeometriDenetimi.cs)
        var hata = await denetim.Denetle(dto.ProjeId, dto.Konum, "Menhol");
        if (hata is not null) return BadRequest(hata);

        // Kod butun projelerde tek olmali (Services/KodDenetimi.cs)
        var kodHatasi = await kodDenetimi.Denetle("Menhol", dto.Kod);
        if (kodHatasi is not null) return BadRequest(kodHatasi);

        var menhol = new Menhol { ProjeId = dto.ProjeId, Konum = dto.Konum, Kod = dto.Kod, Derinlik = dto.Derinlik };
        db.Menholler.Add(menhol);
        await db.SaveChangesAsync();
        return Ok(new { menhol.Id, menhol.Konum, menhol.Kod, menhol.Derinlik });
    }

    // Govde: Ekle ile ayni sekil; ProjeId ve Konum degistirilemez (goz ardi edilir).
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Guncelle(Guid id, [FromBody] MenholEkleDto dto)
    {
        var menhol = await db.Menholler.FindAsync(id);
        if (menhol is null) return NotFound();

        // Kod butun projelerde tek olmali (Services/KodDenetimi.cs)
        var kodHatasi = await kodDenetimi.Denetle("Menhol", dto.Kod, id);
        if (kodHatasi is not null) return BadRequest(kodHatasi);

        menhol.Kod = dto.Kod;
        menhol.Derinlik = dto.Derinlik;
        await db.SaveChangesAsync();
        return Ok(new { menhol.Id, menhol.Konum, menhol.Kod, menhol.Derinlik });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var menhol = await db.Menholler.FindAsync(id);
        if (menhol is null) return NotFound();
        // Bu nesneye bagli fiberler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.BaslangicId == id || f.BitisId == id));
        db.Menholler.Remove(menhol);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record MenholEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Konum zorunlu.")]
    string Konum,

    [Required(ErrorMessage = "Kod zorunlu.")]
    [RegularExpression(@"^MNHL-[A-Z0-9]{7}$", ErrorMessage = "Kod 'MNHL-' + 7 buyuk harf/rakam olmali.")]
    string Kod,

    [Range(0, 100, ErrorMessage = "Derinlik 0-100 m arasi olmali.")]
    decimal Derinlik);
