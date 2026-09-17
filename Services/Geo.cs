using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace WebMap.Services
{
    // Cografi yardimcilar.
    // WKT metnini NetTopologySuite okur (GeometriDenetimi ile ayni kutuphane).
    // Uzunluk NTS'e HESAPLATILMAZ: koordinatlar boylam/enlem oldugu icin geometry.Length
    // DERECE doner (ornegin 0.0009), metre degil. Metre icin iki nokta arasi Haversine kullanilir.
    public static class Geo
    {
        // LINESTRING WKT -> toplam uzunluk (metre). Haversine; sehir olcegi mesafelerde yeterli.
        // Okunamayan metin 0 doner: maliyet raporu tek bozuk kayit yuzunden patlamasin.
        public static double LineStringUzunlukMetre(string wkt)
        {
            Coordinate[] k;
            try { k = new WKTReader().Read(wkt).Coordinates; }   // WKTReader thread-safe degil: cagri basina yeni ornek
            catch { return 0; }

            double toplam = 0;
            for (int i = 1; i < k.Length; i++)
                toplam += Haversine(k[i - 1], k[i]);
            return toplam;
        }

        // Koordinatlarda X = boylam, Y = enlem.
        static double Haversine(Coordinate a, Coordinate b)
        {
            const double R = 6_371_000; // Dunya yaricapi (m)
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
