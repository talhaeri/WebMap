using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers;

[ApiController]
[Route("api/menholler")]
public class MenhollerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Menholler
            .Select(m => new { m.Id, m.Konum, m.Kod, m.Derinlik })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "konum": "POINT(lon lat)", "kod": "...", "derinlik": 1.5 }
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] MenholEkleDto dto)
    {
        var menhol = new Menhol { Konum = dto.Konum, Kod = dto.Kod, Derinlik = dto.Derinlik };
        db.Menholler.Add(menhol);
        await db.SaveChangesAsync();
        return Ok(new { menhol.Id, menhol.Konum, menhol.Kod, menhol.Derinlik });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
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

public record MenholEkleDto(string Konum, string Kod, decimal Derinlik);
