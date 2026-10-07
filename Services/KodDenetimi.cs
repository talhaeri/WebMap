using Microsoft.EntityFrameworkCore;
using WebMap.Data;

namespace WebMap.Services
{
    // Menhol, Kabin ve Santral kodu bütün projelerde tek olmalı (saha envanter kodu).
    // Kod önekleri farklı (MNHL-, KBN-, SNTR-) olduğu için türler birbiriyle çakışmaz; her tür kendi tablosunda aranır.
    public class KodDenetimi(AppDbContext db)
    {
        // Uygunsa null, değilse kullanıcıya gösterilecek hata metni döner.
        // haricId: güncellemede nesnenin kendisi (kendi koduyla çakışmış sayılmasın)
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
