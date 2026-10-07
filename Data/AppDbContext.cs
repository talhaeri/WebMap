using Microsoft.EntityFrameworkCore;
using WebMap.Models;
using WebMap.Services;

namespace WebMap.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, KullaniciBaglami kullaniciBaglami) : DbContext(options)
    {
        // Oturumdaki kullanıcı: query filter'lar ve ProjeKilidiInterceptor bunu kullanır
        public KullaniciBaglami KullaniciBaglami { get; } = kullaniciBaglami;

        bool SadeceOnaylilar => ProjeKurallari.SadeceOnaylilariGorur(KullaniciBaglami.Rol);

        public DbSet<NetworkElement> NetworkElements => Set<NetworkElement>();
        public DbSet<Kabin> Kabinler => Set<Kabin>();
        public DbSet<Menhol> Menholler => Set<Menhol>();
        public DbSet<Santral> Santraller => Set<Santral>();
        public DbSet<Konut> Konutlar => Set<Konut>();
        public DbSet<Fiber> Fiberler => Set<Fiber>();
        public DbSet<Proje> Projeler => Set<Proje>();
        public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
        public DbSet<BirimMaliyet> BirimMaliyetler => Set<BirimMaliyet>();
        public DbSet<ProjeGecmisi> ProjeGecmisi => Set<ProjeGecmisi>();

        // Bir nesneye (başlangıç ya da bitiş olarak) bağlı fiberler
        public IQueryable<Fiber> BagliFiberler(Guid nesneId) =>
            Fiberler.Where(f => f.BaslangicId == nesneId || f.BitisId == nesneId);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Menhol ve Kabin: NetworkElements tablosundan türeyen ayrı tablolar (TPT)
            modelBuilder.Entity<NetworkElement>(e => { e.UseTptMappingStrategy(); e.ToTable("NetworkElements"); });
            modelBuilder.Entity<Kabin>().ToTable("Kabinler");
            modelBuilder.Entity<Menhol>(e =>
            {
                e.ToTable("Menholler");
                e.Property(m => m.Derinlik).HasPrecision(9, 2);   // SQL Server'da decimal için hassasiyet açıkça verilmeli
            });

            modelBuilder.Entity<Kullanici>(e =>
            {
                e.ToTable("Kullanicilar");
                e.Property(k => k.KullaniciAdi).HasMaxLength(50);
                e.Property(k => k.Yetki).HasMaxLength(20);
                e.HasIndex(k => k.KullaniciAdi).IsUnique();
            });

            // Her nesne türü için tek satır. Rakamlar yer tutucudur; ölçüler için bkz. MaliyetHesaplayici.
            modelBuilder.Entity<BirimMaliyet>(e =>
            {
                e.HasIndex(b => b.NesneTuru).IsUnique();
                e.Property(b => b.Iscilik).HasPrecision(18, 2);
                e.Property(b => b.Malzeme).HasPrecision(18, 2);
                e.HasData(
                    new BirimMaliyet { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), NesneTuru = "Menhol", Iscilik = 2000, Malzeme = 3000 },
                    new BirimMaliyet { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), NesneTuru = "Kabin", Iscilik = 4000, Malzeme = 6000 },
                    new BirimMaliyet { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), NesneTuru = "Santral", Iscilik = 20000, Malzeme = 30000 },
                    new BirimMaliyet { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), NesneTuru = "Fiber", Iscilik = 15, Malzeme = 25 });
            });

            modelBuilder.Entity<Proje>(e =>
            {
                e.Property(p => p.ProjeAdi).HasMaxLength(100);
                e.Property(p => p.OlusturanAdi).HasMaxLength(50);
                e.Property(p => p.OnaylayanAdi).HasMaxLength(50);
                e.Property(p => p.RedNotu).HasMaxLength(500);
                e.Property(p => p.Durum).HasMaxLength(20).HasDefaultValue(ProjeDurumlari.Planlama);
                e.ToTable(t => t.HasCheckConstraint("CK_Proje_Durum",
                    $"Durum IN ('{ProjeDurumlari.Planlama}', '{ProjeDurumlari.OnayBekliyor}', '{ProjeDurumlari.Onaylandi}')"));
            });

            modelBuilder.Entity<ProjeGecmisi>(e =>
            {
                e.HasIndex(g => g.ProjeId);
                e.Property(g => g.ProjeAdi).HasMaxLength(100);
                e.Property(g => g.Islem).HasMaxLength(30);
                e.Property(g => g.KullaniciAdi).HasMaxLength(50);
                e.Property(g => g.Not).HasMaxLength(500);
            });

            // Görünürlük: görüntüleme rolü sadece onaylı projeleri ve onlara ait kayıtları görür.
            // Diğer roller (yönetici, düzenleme, onaylayıcı) için filtre etkisizdir.
            modelBuilder.Entity<Proje>().HasQueryFilter(p => !SadeceOnaylilar || p.Durum == ProjeDurumlari.Onaylandi);
            OnayliProjeFiltresi<NetworkElement>(modelBuilder);
            OnayliProjeFiltresi<Santral>(modelBuilder);
            OnayliProjeFiltresi<Konut>(modelBuilder);
            OnayliProjeFiltresi<Fiber>(modelBuilder);
            modelBuilder.Entity<ProjeGecmisi>().HasQueryFilter(g =>   // IProjeyeAit değil, o yüzden ayrı
                !SadeceOnaylilar || Projeler.Any(p => p.Id == g.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));
        }

        // Nesne, bağlı olduğu proje onaylı değilse görüntüleme rolüne görünmez
        void OnayliProjeFiltresi<T>(ModelBuilder modelBuilder) where T : class, IProjeyeAit =>
            modelBuilder.Entity<T>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));
    }
}
