using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Services
{
    // Bos port hesaplanir: kapasite - nesneye bagli fiber sayisi.
    // Fiber silindiginde (dogrudan ya da bagli nesne silindigi icin) port kendiliginden bosalir; tutulacak bir sayac yok.
    // Portu olan tipler: Kabin ve Santral. Menhol ve Konut'ta port kavrami yok.
    public class PortDenetimi (AppDbContext db)
    {
        public async Task<Dictionary<Guid, int>>BosPortlar(Guid projeId)
        {
            var bos = new Dictionary<Guid, int>();
            foreach (var kabin in await db.Kabinler.Where(k => k.ProjeId == projeId).Select(k => new { k.Id, kapasite= k.KabinKapasitesi }).ToListAsync())
            {
                bos[kabin.Id] = kabin.kapasite;
            }

            foreach (var s in await db.Santraller.Where(s => s.ProjeId == projeId).Select(s => new { s.Id, s.Kapasite }).ToListAsync())
            { 
                bos[s.Id] = s.Kapasite;
            }
            var uclar = await db.Fiberler.Where(f => f.ProjeId == projeId).Select(f => new { f.BaslangicId, f.BitisId }).ToListAsync();

            //portu olmayan uclar bulunmadığından otomatik atlanır.
            foreach (var u in uclar)
            {
                if (bos.ContainsKey(u.BaslangicId)) 
                    bos[u.BaslangicId]--;
                if (bos.ContainsKey(u.BitisId)) 
                    bos[u.BitisId]--;
            }
            return bos;
        }

        //fiber eklemeden önce iki uçta da boş port kontrolü
        public async Task<string?> Denetle(Guid projeId, Guid baslangicId, Guid bitisId)
        {
            var bos = await BosPortlar(projeId);

            if (bos.TryGetValue(baslangicId, out var b) && b <= 0)
                return "Fiber baslangicinda bos port kalmadi.";
            if (bos.TryGetValue(bitisId, out var s) && s <= 0)
                return "Fiber bitisinde bos port kalmadi.";
            return null;
        }

        // Bir nesneye bagli fiber sayisi
        public Task<int> FiberSayisi(Guid nesneId) => db.Fiberler.CountAsync(f => f.BaslangicId == nesneId || f.BitisId == nesneId);
    }
}
