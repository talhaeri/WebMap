using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/fiberler")]
public class FiberlerController(AppDbContext db, GeometriDenetimi denetim, PortDenetimi port) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] Guid projeId)
    {
        var liste = await db.Fiberler
            .Where(f => f.ProjeId == projeId)
            .Select(f => new { f.Id, f.Guzergah, f.BaslangicId, f.BitisId })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "projeId": "<guid>", "guzergah": "LINESTRING(...)", "baslangicId": "<guid>", "bitisId": "<guid>" }
    // Kural: baslangic bir Menhol/Kabin/Santral olmali; bitis bir NetworkElement, Konut ya da Santral olmali.
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] FiberEkleDto dto)
    {
        // Proje siniri + poligon ustune yerlesim kurallari (Services/GeometriDenetimi.cs)
        var hata = await denetim.Denetle(dto.ProjeId, dto.Guzergah, "Fiber");
        if (hata is not null) return BadRequest(hata);

        // baslangic: Menhol / Kabin (NetworkElement) VEYA Santral (bagimsiz alan)
        var baslangic = await db.NetworkElements.FindAsync(dto.BaslangicId);
        var baslangicSantral = await db.Santraller.AnyAsync(s => s.Id == dto.BaslangicId);
        if (baslangic is not (Menhol or Kabin) && !baslangicSantral)
            return BadRequest("Fiber baslangici bir menhol, kabin veya santral olmali.");

        // bitis: NetworkElement VEYA Konut VEYA Santral
        var bitisNetworkElement = await db.NetworkElements.AnyAsync(n => n.Id == dto.BitisId);
        var bitisKonut = await db.Konutlar.AnyAsync(k => k.Id == dto.BitisId);
        var bitisSantral = await db.Santraller.AnyAsync(s => s.Id == dto.BitisId);
        if (!bitisNetworkElement && !bitisKonut && !bitisSantral)
            return BadRequest("Fiber bitisi bir network element, konut ya da santral olmali.");
        
        var portHatasi = await port.Denetle(dto.ProjeId, dto.BaslangicId, dto.BitisId);
        if (portHatasi is not null) 
            return BadRequest(portHatasi);

        var fiber = new Fiber
        {
            ProjeId = dto.ProjeId,
            Guzergah = dto.Guzergah,
            BaslangicId = dto.BaslangicId,
            BitisId = dto.BitisId
        };
        db.Fiberler.Add(fiber);
        await db.SaveChangesAsync();
        return Ok(new { fiber.Id, fiber.Guzergah, fiber.BaslangicId, fiber.BitisId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var fiber = await db.Fiberler.FindAsync(id);
        if (fiber is null) return NotFound();
        db.Fiberler.Remove(fiber);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record FiberEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Guzergah zorunlu.")]
    string Guzergah,

    Guid BaslangicId,
    Guid BitisId);
