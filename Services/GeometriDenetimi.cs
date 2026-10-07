using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using WebMap.Data;

namespace WebMap.Services
{
    // Nesne yerleşim kuralları. Geometriler WKT metni olarak saklanır; burada NetTopologySuite ile okunup denetlenir.
    //   1) Her nesne proje sınırının içinde olmalı.
    //   2) Poligonların (Santral, Konut) üzerine nesne konamaz. Kabin muaf, Fiber de muaf (poligona ulaşmak zorunda).
    //   3) Kabin santral üzerindeyse OLT, değilse OLT dışı bir tip olmalı.
    // Aynı kurallar tarayıcıda da var (wwwroot/js/map.js): orası anlık geri bildirim, son söz burası.
    public class GeometriDenetimi(AppDbContext db)
    {
        // Uygunsa null, değilse kullanıcıya gösterilecek hata metni döner.
        public async Task<string?> Denetle(Guid projeId, string wkt, string tur, string? kabinTipi = null)
        {
            var projeWkt = await db.Projeler
                .Where(p => p.Id == projeId)
                .Select(p => p.Geometri)
                .FirstOrDefaultAsync();
            if (projeWkt is null) return "Gecerli bir proje secilmeli.";

            var okuyucu = new WKTReader();   // thread-safe değil: her çağrıda yeni örnek
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

            // Covers (Contains değil): sınırın tam üzerindeki nokta da içeride sayılır
            if (!proje.Covers(sekil)) return $"{tur} proje alani disina eklenemez.";

            if (tur == "Fiber") return null;

            var santralUstunde = false;
            foreach (var (poligonTuru, poligonWkt) in await Poligonlar(projeId))
            {
                Geometry poligon;
                try { poligon = okuyucu.Read(poligonWkt); } catch { continue; }

                // Nokta için: poligona değiyor mu. Poligon için: iç alanlar çakışıyor mu (sadece kenar teması çakışma değil).
                var carpisma = sekil is Point
                    ? poligon.Intersects(sekil)
                    : poligon.Intersects(sekil) && !poligon.Touches(sekil);
                if (!carpisma) continue;

                if (tur != "Kabin") return $"{tur} bir {poligonTuru} uzerine konulamaz.";
                if (poligonTuru == "Santral") santralUstunde = true;
            }

            if (tur == "Kabin")
            {
                if (santralUstunde && kabinTipi != "OLT")
                    return "Santral üzerine sadece OLT tipi kabin konulabilir!";
                if (!santralUstunde && kabinTipi == "OLT")
                    return "OLT tipi kabin sadece santral üzerine konulabilir!";
            }
            return null;
        }

        // Projedeki poligon nesneler (tür adıyla birlikte)
        async Task<List<(string Tur, string Wkt)>> Poligonlar(Guid projeId)
        {
            var santraller = await db.Santraller.Where(s => s.ProjeId == projeId).Select(s => s.Geometri).ToListAsync();
            var konutlar = await db.Konutlar.Where(k => k.ProjeId == projeId).Select(k => k.Geometri).ToListAsync();
            return [.. santraller.Select(w => ("Santral", w)), .. konutlar.Select(w => ("Konut", w))];
        }
    }
}
