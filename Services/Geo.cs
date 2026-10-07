using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace WebMap.Services
{
    // Coğrafi yardımcılar. Koordinatlar boylam/enlem olduğu için NTS'in Length'i metre değil derece verir;
    // metre için ardışık noktalar arası Haversine kullanılır.
    public static class Geo
    {
        // LINESTRING WKT'nin toplam uzunluğu (metre).
        // Okunamayan metin 0 döner: tek bozuk kayıt maliyet raporunu bozmasın.
        public static double LineStringUzunlukMetre(string wkt)
        {
            Coordinate[] k;
            try { k = new WKTReader().Read(wkt).Coordinates; }   // WKTReader thread-safe değil: her çağrıda yeni örnek
            catch { return 0; }

            return k.Zip(k.Skip(1), Haversine).Sum();
        }

        // X = boylam, Y = enlem
        static double Haversine(Coordinate a, Coordinate b)
        {
            const double R = 6_371_000;   // Dünya yarıçapı (m)
            double dLat = Rad(b.Y - a.Y);
            double dLon = Rad(b.X - a.X);
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(Rad(a.Y)) * Math.Cos(Rad(b.Y))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        static double Rad(double deg) => deg * Math.PI / 180;
    }
}
