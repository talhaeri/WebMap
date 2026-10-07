using Microsoft.EntityFrameworkCore;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Services
{
    // Rapordaki bir satır. Çarpan: işçilik/malzeme hesabında kullanılan miktar (adet, toplam derinlik m ya da toplam güzergah m).
    // DİKKAT: alan adları onay anında veritabanına JSON olarak kaydedilir (Proje.OnayMaliyeti); adı değişirse eski onaylar okunamaz.
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

    // Bütün kalemler ve genel toplamlar
    public record MaliyetSonucu(
        List<MaliyetKalemi> Kalemler,
        decimal IscilikGenelToplam,
        decimal MalzemeGenelToplam,
        decimal GenelToplam
        );

    // Proje maliyeti. Birim fiyatın ölçüsü türe göre değişir:
    //   Kabin, Santral : adet x birim (işçilik ve malzeme)
    //   Menhol         : işçilik = toplam derinlik (m) x birim, malzeme = adet x birim
    //   Fiber          : işçilik ve malzeme = toplam güzergah uzunluğu (m) x birim
    public class MaliyetHesaplayici(AppDbContext db)
    {
        public async Task<MaliyetSonucu> Hesapla(Guid projeId)
        {
            var birim = await db.BirimMaliyetler.ToDictionaryAsync(b => b.NesneTuru);

            var menholler = db.Menholler.Where(m => m.ProjeId == projeId);
            var menholAdet = await menholler.CountAsync();
            var derinlikToplam = await menholler.SumAsync(m => m.Derinlik);

            var kabinAdet = await db.Kabinler.CountAsync(k => k.ProjeId == projeId);
            var santralAdet = await db.Santraller.CountAsync(s => s.ProjeId == projeId);

            var fiberler = db.Fiberler.Where(f => f.ProjeId == projeId);
            var fiberAdet = await fiberler.CountAsync();
            var guzergahlar = await fiberler.Select(f => f.Guzergah).ToListAsync();
            var uzunlukToplam = Math.Round((decimal)guzergahlar.Sum(Geo.LineStringUzunlukMetre), 2);

            MaliyetKalemi Kalem(string tur, int adet, decimal iscCarpani, decimal malzCarpani, string iscOlcusu, string malzOlcusu)
            {
                var fiyat = birim.GetValueOrDefault(tur) ?? new BirimMaliyet();   // fiyat tanımlı değilse 0
                var iscToplam = Math.Round(iscCarpani * fiyat.Iscilik, 2);
                var malzToplam = Math.Round(malzCarpani * fiyat.Malzeme, 2);
                return new MaliyetKalemi(
                    tur, adet, iscCarpani, malzCarpani,
                    fiyat.Iscilik, fiyat.Malzeme,
                    iscToplam, malzToplam, iscToplam + malzToplam,
                    iscOlcusu, malzOlcusu);
            }

            var kalemler = new List<MaliyetKalemi>
            {
                Kalem("Menhol", menholAdet, derinlikToplam, menholAdet, "m", "adet"),
                Kalem("Kabin", kabinAdet, kabinAdet, kabinAdet, "adet", "adet"),
                Kalem("Santral", santralAdet, santralAdet, santralAdet, "adet", "adet"),
                Kalem("Fiber", fiberAdet, uzunlukToplam, uzunlukToplam, "m", "m"),
            };

            return new MaliyetSonucu(
                kalemler,
                kalemler.Sum(k => k.IscilikToplam),
                kalemler.Sum(k => k.MalzemeToplam),
                kalemler.Sum(k => k.Toplam));
        }
    }
}
