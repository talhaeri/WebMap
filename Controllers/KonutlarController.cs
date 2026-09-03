using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers;

[ApiController]
[Route("api/konutlar")]
public class KonutlarController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Konutlar
            .Select(k => new { k.Id, k.Geometri, k.UAVTKod, k.BBKsayi })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "geometri": "POLYGON((...))", "uavtKod": 123, "bbKsayi": 8 }
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] KonutEkleDto dto)
    {
        var konut = new Konut { Geometri = dto.Geometri, UAVTKod = dto.UAVTKod, BBKsayi = dto.BBKsayi };
        db.Konutlar.Add(konut);
        await db.SaveChangesAsync();
        return Ok(new { konut.Id, konut.Geometri, konut.UAVTKod, konut.BBKsayi });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
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

public record KonutEkleDto(string Geometri, int UAVTKod, int BBKsayi);
