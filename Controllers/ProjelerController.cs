using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Controllers;

[ApiController]
[Route("api/projeler")]
public class ProjelerController(AppDbContext db, MaliyetHesaplayici maliyetHesaplayici) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var liste = await db.Projeler
            .Select(p => new { p.Id, p.Ad, p.Geometri, p.OlusturmaTarihi })
            .ToListAsync();
        return Ok(liste);
    }

    // Govde: { "ad": "...", "geometri": "POLYGON((...))" }
    [HttpPost]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Ekle([FromBody] ProjeEkleDto dto)
    {
        var proje = new Proje { Ad = dto.Ad, Geometri = dto.Geometri };
        db.Projeler.Add(proje);
        await db.SaveChangesAsync();
        return Ok(new { proje.Id, proje.Ad, proje.Geometri, proje.OlusturmaTarihi });
    }

    [HttpGet("{id:guid}/maliyet")]
    public async Task<IActionResult> Maliyet(Guid id)
    {
        if (!await db.Projeler.AnyAsync(p => p.Id == id)) return NotFound();
        return Ok(await maliyetHesaplayici.Hesapla(id));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Yetkiler.Duzenleyebilir)]
    public async Task<IActionResult> Sil(Guid id)
    {
        var proje = await db.Projeler.FindAsync(id);
        if (proje is null) return NotFound();

        // Projeye bagli tum nesneler de silinir (FK yok, elle).
        db.Fiberler.RemoveRange(db.Fiberler.Where(f => f.ProjeId == id));
        db.Menholler.RemoveRange(db.Menholler.Where(m => m.ProjeId == id));
        db.Kabinler.RemoveRange(db.Kabinler.Where(k => k.ProjeId == id));
        db.Santraller.RemoveRange(db.Santraller.Where(s => s.ProjeId == id));
        db.Konutlar.RemoveRange(db.Konutlar.Where(k => k.ProjeId == id));

        db.Projeler.Remove(proje);
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record ProjeEkleDto(
    [Required(ErrorMessage = "Ad zorunlu.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter.")]
    string Ad,

    [Required(ErrorMessage = "Geometri zorunlu.")]
    string Geometri);
