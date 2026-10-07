using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebMap.Data;
using WebMap.Models;

namespace WebMap.Services
{
    // Proje kilidi: projenin içindeki nesnelere (IProjeyeAit) yazılmadan önce projenin durumuna bakılır.
    // Controller'lara tek tek yazmak yerine SaveChanges'in tek kapısında durur; yeni uçlar da otomatik korunur.
    //   Kural   : ProjeKurallari.NesneDuzenleyebilir(rol, durum)
    //   İstisna : aynı kayıtta silinen projenin nesnelerine bakılmaz (proje silme; o uç sadece yöneticide)
    //   Geçmiş  : Planlama dışındaki projede yapılan değişiklik (pratikte yönetici, OnayBekliyor) ProjeGecmisi'ne yazılır
    // Durum tutmaz, tek örnek (singleton) yeter. Kullanıcıyı DbContext'ten okur.
    // DİKKAT: ExecuteUpdate ve ExecuteDelete SaveChanges'e uğramaz, kilidi atlar; nesne tablolarında kullanılmamalı.
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

        // Eklenen, değişen ve silinen proje nesneleri (aynı kayıtta silinen projelerinkiler hariç)
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

        // İlgili projelerin veritabanındaki hali. Görünürlük filtresi atlanır:
        // kilit, kullanıcının gördüğüne değil projenin gerçek durumuna bakmalı.
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

                // Planlama'daki olağan çalışma geçmişe yazılmaz, sadece kilitli projedeki değişiklik
                if (proje.Durum != ProjeDurumlari.Planlama)
                    db.ProjeGecmisi.Add(ProjeGecmisi.Yeni(proje.Id, proje.ProjeAdi, ProjeIslemleri.Degistir,
                        kullanici.Id, kullanici.Ad, Ozet(degisenler.Where(x => x.Entity.ProjeId == proje.Id))));
            }
        }

        // Örnek: "1 Menhol silindi, 2 Fiber eklendi". Tek tek değil adetle: Not kolonu 500 karakter.
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
