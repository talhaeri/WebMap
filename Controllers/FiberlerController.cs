using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // Gövde: { "projeId": "<guid>", "guzergah": "LINESTRING(...)", "baslangicId": "<guid>", "bitisId": "<guid>" }
    // Başlangıç menhol, kabin ya da santral olmalı; bitiş bunlardan biri ya da konut olmalı.
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] FiberEkleDto dto)
    {
        // Aynı nesnede başlayıp biten fiber anlamsız; üstelik o nesneden iki port düşer
        if (dto.BaslangicId == dto.BitisId)
            return BadRequest("Fiber başlangıcı ve bitişi aynı nesne olamaz.");

        var hata = await denetim.Denetle(dto.ProjeId, dto.Guzergah, "Fiber");   // proje sınırı
        if (hata is not null) return BadRequest(hata);

        if (!await AgNesnesiMi(dto.BaslangicId))
            return BadRequest("Fiber baslangici bir menhol, kabin veya santral olmali.");
        if (!await AgNesnesiMi(dto.BitisId) && !await db.Konutlar.AnyAsync(k => k.Id == dto.BitisId))
            return BadRequest("Fiber bitisi bir network element, konut ya da santral olmali.");

        var portHatasi = await port.Denetle(dto.ProjeId, dto.BaslangicId, dto.BitisId);
        if (portHatasi is not null) return BadRequest(portHatasi);

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

    // Menhol ya da kabin (NetworkElement) ya da santral mı
    async Task<bool> AgNesnesiMi(Guid id) =>
        await db.NetworkElements.AnyAsync(n => n.Id == id) || await db.Santraller.AnyAsync(s => s.Id == id);
}

public record FiberEkleDto(
    Guid ProjeId,

    [Required(ErrorMessage = "Guzergah zorunlu.")]
    string Guzergah,

    Guid BaslangicId,
    Guid BitisId);
