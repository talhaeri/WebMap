using Microsoft.EntityFrameworkCore;
using WebMap.Data;

namespace WebMap.Services
{
    // Boş port = kapasite - nesneye bağlı fiber sayısı. Sayaç tutulmaz: fiber silinince port kendiliğinden boşalır.
    // Portu olan tipler: Kabin ve Santral (Menhol ve Konut'ta port yok).
    public class PortDenetimi(AppDbContext db)
    {
        // Projedeki kabin ve santralların boş port sayısı (anahtar: nesne Id'si)
        public async Task<Dictionary<Guid, int>> BosPortlar(Guid projeId)
        {
            var kabinler = await db.Kabinler.Where(k => k.ProjeId == projeId)
                .Select(k => new { k.Id, Kapasite = k.KabinKapasitesi }).ToListAsync();
            var santraller = await db.Santraller.Where(s => s.ProjeId == projeId)
                .Select(s => new { s.Id, s.Kapasite }).ToListAsync();
            var bos = kabinler.Concat(santraller).ToDictionary(x => x.Id, x => x.Kapasite);

            var fiberler = await db.Fiberler.Where(f => f.ProjeId == projeId)
                .Select(f => new { f.BaslangicId, f.BitisId }).ToListAsync();
            foreach (var f in fiberler)
            {
                // Portu olmayan uçlar (menhol, konut) sözlükte yoktur, atlanır
                if (bos.ContainsKey(f.BaslangicId)) bos[f.BaslangicId]--;
                if (bos.ContainsKey(f.BitisId)) bos[f.BitisId]--;
            }
            return bos;
        }

        // Fiber eklemeden önce iki uçta da boş port var mı. Uygunsa null döner.
        public async Task<string?> Denetle(Guid projeId, Guid baslangicId, Guid bitisId)
        {
            var bos = await BosPortlar(projeId);

            if (bos.TryGetValue(baslangicId, out var b) && b <= 0)
                return "Fiber baslangicinda bos port kalmadi.";
            if (bos.TryGetValue(bitisId, out var s) && s <= 0)
                return "Fiber bitisinde bos port kalmadi.";
            return null;
        }

        // Bir nesneye bağlı fiber sayısı
        public Task<int> FiberSayisi(Guid nesneId) => db.BagliFiberler(nesneId).CountAsync();
    }
}
