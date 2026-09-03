using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers;

[ApiController]
[Route("api/ticariler")]
public class TicarilerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Ticariler
            .Select(t => new { t.Id, t.Geometri, t.UAVTKod, t.IsyeriSayisi })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "geometri": "POLYGON((...))", "uavtKod": 123, "isyeriSayisi": 5 }
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] TicariEkleDto dto)
    {
        var ticari = new Ticari { Geometri = dto.Geometri, UAVTKod = dto.UAVTKod, IsyeriSayisi = dto.IsyeriSayisi };
        db.Ticariler.Add(ticari);
        await db.SaveChangesAsync();
        return Ok(new { ticari.Id, ticari.Geometri, ticari.UAVTKod, ticari.IsyeriSayisi });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
    {
        var ticari = await db.Ticariler.FindAsync(id);
        if (ticari is null) return NotFound();
        // Bu nesneye bagli fiberler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.BaslangicId == id || f.BitisId == id));
        db.Ticariler.Remove(ticari);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record TicariEkleDto(string Geometri, int UAVTKod, int IsyeriSayisi);
