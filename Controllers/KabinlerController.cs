using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Controllers;

[ApiController]
[Route("api/kabinler")]
public class KabinlerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Kabinler
            .Select(k => new { k.Id, k.Konum, k.Kod, k.KabinTipi, k.KabinKapasitesi, k.BosPort })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "konum": "POINT(lon lat)", "kod": "...", "kabinTipi": "...", "kabinKapasitesi": 288, "bosPort": 288 }
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] KabinEkleDto dto)
    {
        var kabin = new Kabin
        {
            Konum = dto.Konum,
            Kod = dto.Kod,
            KabinTipi = dto.KabinTipi,
            KabinKapasitesi = dto.KabinKapasitesi,
            BosPort = dto.BosPort
        };
        db.Kabinler.Add(kabin);
        await db.SaveChangesAsync();
        return Ok(new { kabin.Id, kabin.Konum, kabin.Kod, kabin.KabinTipi, kabin.KabinKapasitesi, kabin.BosPort });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
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

public record KabinEkleDto(string Konum, string Kod, string KabinTipi, int KabinKapasitesi, int BosPort);
