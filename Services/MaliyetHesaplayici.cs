using Microsoft.EntityFrameworkCore;
using WebMap.Data;

namespace WebMap.Services
{
    // Bir kalem: nesne turu, projedeki adet, iscilik/malzeme hesabinda kullanilan carpan
    // (adet | toplam derinlik m | toplam guzergah m), birim fiyatlar ve toplamlar.
    public record MaliyetKalemi(
        string NesneTuru,
        int Adet,
        decimal IscilikCarpani,
        decimal MalzemeCarpani,
        decimal IscilikBirim,
        decimal MalzemeBirim,
        decimal IscilikToplam,
        decimal MalzemeToplam,
        decimal Toplam,
        string IscilikOlcusu,
        string MalzemeOlcusu
        );

    // Butun kalemler + iscilik/malzeme genel toplamlari + genel toplam.
    public record MaliyetSonucu(
        List<MaliyetKalemi> Kalemler,
        decimal IscilikGenelToplam,
        decimal MalzemeGenelToplam,
        decimal GenelToplam
        );

    // Proje maliyeti. Birim fiyatin OLCUSU ture gore degisir:
    //   Kabin / Santral : adet x birim           (iscilik + malzeme)
    //   Menhol          : iscilik = toplam derinlik (m) x birim ; malzeme = adet x birim
    //   Fiber           : iscilik ve malzeme = toplam guzergah uzunlugu (m) x birim
    // Controller'da DEGIL burada hesaplanir (controller sadece bu servisi cagirir).
    public class MaliyetHesaplayici(AppDbContext db)
    {
        public async Task<MaliyetSonucu> Hesapla(Guid projeId)
        {
            var birim = await db.BirimMaliyetler.ToDictionaryAsync(b => b.NesneTuru);
            decimal IscBirim(string t) => birim.GetValueOrDefault(t)?.Iscilik ?? 0m;
            decimal MalzBirim(string t) => birim.GetValueOrDefault(t)?.Malzeme ?? 0m;

            var menholAdet = await db.Menholler.CountAsync(m => m.ProjeId == projeId);
            var kabinAdet = await db.Kabinler.CountAsync(k => k.ProjeId == projeId);
            var santralAdet = await db.Santraller.CountAsync(s => s.ProjeId == projeId);
            var fiberAdet = await db.Fiberler.CountAsync(f => f.ProjeId == projeId);

            // Menhol iscilik carpani: projedeki tum menhollerin derinlik toplami (m).
            var menholDerinlikToplam = menholAdet == 0
                ? 0m
                : await db.Menholler.Where(m => m.ProjeId == projeId).SumAsync(m => m.Derinlik);

            // Fiber carpani: tum guzergahlarin uzunluk toplami (m).
            var guzergahlar = await db.Fiberler
                .Where(f => f.ProjeId == projeId)
                .Select(f => f.Guzergah)
                .ToListAsync();
            var fiberUzunlukToplam = Math.Round((decimal)guzergahlar.Sum(Geo.LineStringUzunlukMetre), 2);

            MaliyetKalemi Kalem(string tur, int adet, decimal iscCarpani, decimal malzCarpani, string iscOlcusu, string malzOlcusu)
            {
                var iscToplam = Math.Round(iscCarpani * IscBirim(tur), 2);
                var malzToplam = Math.Round(malzCarpani * MalzBirim(tur), 2);
                return new MaliyetKalemi(
                    tur, adet, iscCarpani, malzCarpani,
                    IscBirim(tur), MalzBirim(tur),                  
                    iscToplam, malzToplam, iscToplam + malzToplam,  
                    iscOlcusu, malzOlcusu);
            }

            var kalemler = new List<MaliyetKalemi>
            {
                Kalem("Menhol", menholAdet, menholDerinlikToplam, menholAdet, "m", "adet"),   // iscilik: derinlik, malzeme: adet
                Kalem("Kabin", kabinAdet, kabinAdet, kabinAdet, "adet", "adet"),
                Kalem("Santral", santralAdet, santralAdet, santralAdet, "adet", "adet"),
                Kalem("Fiber", fiberAdet, fiberUzunlukToplam, fiberUzunlukToplam, "m", "m"), // iscilik + malzeme: uzunluk
            };

            return new MaliyetSonucu(
                kalemler,
                kalemler.Sum(k => k.IscilikToplam),
                kalemler.Sum(k => k.MalzemeToplam),
                kalemler.Sum(k => k.Toplam));
        }
    }
}
