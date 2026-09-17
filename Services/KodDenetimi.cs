using Microsoft.EntityFrameworkCore;
using WebMap.Data;

namespace WebMap.Services
{
    // Menhol / Kabin / Santral kodu BUTUN projelerde tek olmali (saha envanter kodu).
    // Turler kendi aralarinda cakisamaz (onekler farkli: MNHL- / KBN- / SNTR-), her tur kendi tablosunda aranir.
    // Uygunsa null, degilse kullaniciya gosterilecek hata metni doner.
    public class KodDenetimi(AppDbContext db)
    {
        // haricId: guncellemede nesnenin kendisi (kendi koduyla cakisma sayilmasin).
        public async Task<string?> Denetle(string tur, string kod, Guid? haricId = null)
        {
            var projeId = tur switch
            {
                "Menhol" => await db.Menholler.Where(x => x.Kod == kod && x.Id != haricId).Select(x => (Guid?)x.ProjeId).FirstOrDefaultAsync(),
                "Kabin" => await db.Kabinler.Where(x => x.Kod == kod && x.Id != haricId).Select(x => (Guid?)x.ProjeId).FirstOrDefaultAsync(),
                "Santral" => await db.Santraller.Where(x => x.Kod == kod && x.Id != haricId).Select(x => (Guid?)x.ProjeId).FirstOrDefaultAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(tur), tur, "Kodu olan turler: Menhol, Kabin, Santral")
            };
            if (projeId is null) return null;

            var projeAdi = await db.Projeler.Where(p => p.Id == projeId).Select(p => p.ProjeAdi).FirstOrDefaultAsync();
            return $"{kod} kodu \"{projeAdi}\" projesinde zaten kullanılıyor.";
        }
    }
}
