using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebMap.Data;
using WebMap.Models;
using static WebMap.Models.ProjeSabitler;

namespace WebMap.Services
{
    // Proje kilidi: bir projenin ICINDEKI nesnelere (IProjeyeAit) yazilmadan once projenin durumuna bakilir.
    // Controller'lara tek tek yazmak yerine SaveChanges'in tek kapisinda durur; yeni eklenecek uclar da korunur.
    //
    //   Kural   : ProjeKurallari.NesneDuzenleyebilir(rol, durum) - kural tablosu tek yerde.
    //   Istisna : Ayni kayitta silinen projenin nesnelerine bakilmaz (proje silme; o uc sadece yoneticide).
    //   Gecmis  : Planlama disindaki projede yapilan degisiklik (pratikte: yonetici, OnayBekliyor) ProjeGecmisi'ne yazilir.
    //
    // Durum tutmaz, tek ornek (singleton) butun isteklerde kullanilir. Kullaniciyi DbContext'ten okur.
    // DIKKAT: ExecuteUpdate / ExecuteDelete SaveChanges'e ugramaz, bu kilidi atlar. Nesne tablolarinda kullanilmamali.
    public class ProjeKilidiInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> sonuc)
        {
            var db = (AppDbContext)e.Context!;
            var degisenler = Degisenler(db);
            if (degisenler.Count > 0)
                Denetle(db, degisenler, Projeler(db, degisenler).ToList());
            return sonuc;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData e, InterceptionResult<int> sonuc, CancellationToken ct = default)
        {
            var db = (AppDbContext)e.Context!;
            var degisenler = Degisenler(db);
            if (degisenler.Count > 0)
                Denetle(db, degisenler, await Projeler(db, degisenler).ToListAsync(ct));
            return sonuc;
        }

        // Eklenen / degisen / silinen proje nesneleri (ayni kayitta silinen projelerinkiler haric).
        static List<EntityEntry<IProjeyeAit>> Degisenler(AppDbContext db)
        {
            var silinenProjeler = db.ChangeTracker.Entries<Proje>()
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => x.Entity.Id)
                .ToHashSet();

            return db.ChangeTracker.Entries<IProjeyeAit>()
                .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(x => !silinenProjeler.Contains(x.Entity.ProjeId))
                .ToList();
        }

        // Ilgili projelerin veritabanindaki hali. Gorunurluk filtresi atlanir: kilit, kullanicinin
        // gorebildigine degil projenin gercek durumuna bakmali.
        static IQueryable<Proje> Projeler(AppDbContext db, List<EntityEntry<IProjeyeAit>> degisenler)
        {
            var idler = degisenler.Select(x => x.Entity.ProjeId).Distinct().ToList();
            return db.Projeler.IgnoreQueryFilters().AsNoTracking().Where(p => idler.Contains(p.Id));
        }

        static void Denetle(AppDbContext db, List<EntityEntry<IProjeyeAit>> degisenler, List<Proje> projeler)
        {
            var kullanici = db.KullaniciBaglami;
            foreach (var proje in projeler)
            {
                if (!ProjeKurallari.NesneDuzenleyebilir(kullanici.Rol, proje.Durum))
                    throw new ProjeKilitliException(proje.Durum switch
                    {
                        ProjeDurumlari.OnayBekliyor => $"\"{proje.ProjeAdi}\" projesi onay bekliyor, düzenlenemez.",
                        ProjeDurumlari.Onaylandi => $"\"{proje.ProjeAdi}\" projesi onaylandı. Düzenlemek için önce Planlama'ya alınmalı.",
                        _ => $"\"{proje.ProjeAdi}\" projesini düzenleme yetkiniz yok."
                    });

                // Planlama'daki olagan calisma gecmise yazilmaz; sadece kilitli projedeki degisiklik.
                if (proje.Durum != ProjeDurumlari.Planlama)
                    db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(proje.Id, proje.ProjeAdi, ProjeIslemleri.Degistir,
                        kullanici.Id, kullanici.Ad, Ozet(degisenler.Where(x => x.Entity.ProjeId == proje.Id))));
            }
        }

        // Ornek: "1 Menhol silindi, 2 Fiber silindi". Tek tek degil adetle: Not kolonu 500 karakter.
        static string Ozet(IEnumerable<EntityEntry<IProjeyeAit>> kayitlar) => string.Join(", ", kayitlar
            .GroupBy(x => (Tur: x.Metadata.ClrType.Name, x.State))
            .Select(g => $"{g.Count()} {g.Key.Tur} " + g.Key.State switch
            {
                EntityState.Added => "eklendi",
                EntityState.Deleted => "silindi",
                _ => "güncellendi"
            }));
    }
}
