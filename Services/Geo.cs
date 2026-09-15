using System.Globalization;

namespace WebMap.Services
{
    // Cografi yardimcilar. WKT metinleri wellknown (frontend) formatinda gelir:
    // "LINESTRING (lon lat, lon lat, ...)" - LINESTRING'den sonra bosluk olabilir.
    public static class Geo
    {
        // LINESTRING WKT -> toplam uzunluk (metre). Haversine; sehir olcegi mesafelerde yeterli.
        public static double LineStringUzunlukMetre(string wkt)
        {
            if (string.IsNullOrWhiteSpace(wkt)) return 0;

            var ac = wkt.IndexOf('(');
            var kapa = wkt.LastIndexOf(')');
            if (ac < 0 || kapa <= ac) return 0;

            var noktalar = wkt[(ac + 1)..kapa]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(xy => xy.Length >= 2)
                .Select(xy => (
                    Lon: double.Parse(xy[0], CultureInfo.InvariantCulture),
                    Lat: double.Parse(xy[1], CultureInfo.InvariantCulture)))
                .ToList();

            double toplam = 0;
            for (int i = 1; i < noktalar.Count; i++)
                toplam += Haversine(noktalar[i - 1], noktalar[i]);
            return toplam;
        }

        static double Haversine((double Lon, double Lat) a, (double Lon, double Lat) b)
        {
            const double R = 6_371_000; // Dunya yaricapi (m)
            double dLat = Rad(b.Lat - a.Lat);
            double dLon = Rad(b.Lon - a.Lon);
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(Rad(a.Lat)) * Math.Cos(Rad(b.Lat))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        static double Rad(double deg) => deg * Math.PI / 180;
    }
}
