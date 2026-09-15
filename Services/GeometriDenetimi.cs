    using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Services
{
    // Nesne yerlesim kurallari. Kolon tipleri DEGISMEDI: geometriler hala WKT metni
    // olarak saklaniyor, burada NetTopologySuite ile okunup bellekte denetleniyor.
    //
    //   1) Her nesne proje sinirinin icinde olmali.
    //   2) Poligonlarin (Santral / Konut) uzerine nesne konulamaz.
    //      Istisna Kabin: poligon ustune konabilir.
    //      Istisna Fiber: poligonlara ulasmak zorunda oldugu icin muaf.
    //
    // Ayni kurallar tarayicida da var (wwwroot/js/map.js). Orasi cizim sirasinda
    // anlik geri bildirim icin; son soz burasi, cunku API'ye dogrudan istek atilabilir.
    public class GeometriDenetimi(AppDbContext db)
    {
        // Uygunsa null, degilse kullaniciya gosterilecek hata metni doner.
        public async Task<string?> Denetle(Guid projeId, string wkt, string tur, string? kabinTipi = null)
        {
            var projeWkt = await db.Projeler
                .Where(p => p.Id == projeId)
                .Select(p => p.Geometri)
                .FirstOrDefaultAsync();

            if (projeWkt is null) return "Gecerli bir proje secilmeli.";

            var okuyucu = new WKTReader();   // WKTReader thread-safe degil: cagri basina yeni ornek
            Geometry proje, sekil;
            try
            {
                proje = okuyucu.Read(projeWkt);
                sekil = okuyucu.Read(wkt);
            }
            catch
            {
                return "Geometri okunamadi.";
            }

            if (!sekil.IsValid) return "Gecersiz geometri.";

            // Covers (Contains degil): sinirin tam uzerindeki nokta da iceride sayilir.
            if (!proje.Covers(sekil)) return $"{tur} proje alani disina eklenemez.";

            if (tur == "Fiber") return null;

            var santralUstunde = false;

            foreach (var (poligonTuru, poligonWkt) in await Poligonlar(projeId))
            {
                Geometry poligon;
                try { poligon = okuyucu.Read(poligonWkt); } catch { continue; }

                // Nokta icin: poligona degiyor mu.
                // Poligon icin: ic alanlar cakisiyor mu - sadece kenar temasi (Touches) cakisma sayilmaz.
                var carpisma = sekil is Point
                    ? poligon.Intersects(sekil)
                    : poligon.Intersects(sekil) && !poligon.Touches(sekil);

                if (!carpisma) continue;

                if (tur != "Kabin") return $"{tur} bir {poligonTuru} uzerine konulamaz.";

                if (poligonTuru == "Santral") santralUstunde = true;
            }
            if (tur == "Kabin") {
                if (santralUstunde && kabinTipi != "OLT") 
                    return "Santral üzerine sadece OLT tipi kabin konulabilir!";
                if(!santralUstunde && kabinTipi == "OLT")
                    return "OLT tipi kabin sadece santral üzerine konulabilir!";
            }
            return null;
        }

        // Projedeki tum poligon nesneleri (tur adiyla birlikte).
        async Task<List<(string Tur, string Wkt)>> Poligonlar(Guid projeId)
        {
            var santraller = await db.Santraller
                .Where(s => s.ProjeId == projeId).Select(s => s.Geometri).ToListAsync();
            var konutlar = await db.Konutlar
                .Where(k => k.ProjeId == projeId).Select(k => k.Geometri).ToListAsync();

            var liste = new List<(string Tur, string Wkt)>();
            liste.AddRange(santraller.Select(w => ("Santral", w)));
            liste.AddRange(konutlar.Select(w => ("Konut", w)));
            return liste;
        }

    }
}
