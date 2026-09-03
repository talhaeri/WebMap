using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers;

[ApiController]
[Route("api/fiberler")]
public class FiberlerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Fiberler
            .Select(f => new { f.Id, f.Guzergah, f.BaslangicId, f.BitisId })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "guzergah": "LINESTRING(...)", "baslangicId": 1, "bitisId": 2 }
    // Kural: baslangic bir Menhol/Kabin olmali; bitis bir NetworkElement ya da Alan olmali.
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] FiberEkleDto dto)
    {
        var baslangic = await db.NetworkElements.FindAsync(dto.BaslangicId);
        if (baslangic is not (Menhol or Kabin))
            return BadRequest("Fiber baslangici bir menhol veya kabin olmali.");

        var bitisNetworkElement = await db.NetworkElements.AnyAsync(n => n.Id == dto.BitisId);
        var bitisAlan = await db.Alanlar.AnyAsync(a => a.Id == dto.BitisId);
        if (!bitisNetworkElement && !bitisAlan)
            return BadRequest("Fiber bitisi bir network element ya da alan olmali.");

        var fiber = new Fiber
        {
            Guzergah = dto.Guzergah,
            BaslangicId = dto.BaslangicId,
            BitisId = dto.BitisId
        };
        db.Fiberler.Add(fiber);
        await db.SaveChangesAsync();
        return Ok(new { fiber.Id, fiber.Guzergah, fiber.BaslangicId, fiber.BitisId });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
    {
        var fiber = await db.Fiberler.FindAsync(id);
        if (fiber is null) return NotFound();
        db.Fiberler.Remove(fiber);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record FiberEkleDto(string Guzergah, int BaslangicId, int BitisId);
