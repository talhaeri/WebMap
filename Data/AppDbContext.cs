using Microsoft.EntityFrameworkCore;
using WebMap.Models;
using WebMap.Services;
using static WebMap.Models.ProjeSabitler;

namespace WebMap.Data
{
    public class AppDbContext : DbContext
    {
        readonly KullaniciBaglami kullaniciBaglami;
        public AppDbContext(DbContextOptions<AppDbContext> options, KullaniciBaglami kullaniciBaglami) : base(options)
        {
            this.kullaniciBaglami = kullaniciBaglami;
        }

        // Oturumdaki kullanici. Query filter'lar ve ProjeKilidiInterceptor ayni ornegi kullanir.
        public KullaniciBaglami KullaniciBaglami => kullaniciBaglami;

        bool SadeceOnaylilar => ProjeKurallari.SadeceOnaylilariGorur(kullaniciBaglami.Rol);
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<NetworkElement>().UseTptMappingStrategy();
            modelBuilder.Entity<NetworkElement>().ToTable("NetworkElements");
            modelBuilder.Entity<Kabin>().ToTable("Kabinler");
            modelBuilder.Entity<Menhol>().ToTable("Menholler");
            modelBuilder.Entity<Kullanici>().ToTable("Kullanicilar");

            // SQL Server'da decimal icin hassasiyet acikca verilmeli (yoksa decimal(18,2) + uyari).
            modelBuilder.Entity<Menhol>().Property(m => m.Derinlik).HasPrecision(9, 2);

            // Menhol Iscilik = derinlik-metresi basi, Malzeme = adet basi;
            // Fiber Iscilik + Malzeme = guzergah-metresi basi. Rakamlar PLACEHOLDER.
            modelBuilder.Entity<BirimMaliyet>().HasIndex(b => b.NesneTuru).IsUnique();
            modelBuilder.Entity<BirimMaliyet>().Property(b => b.Iscilik).HasPrecision(18, 2);
            modelBuilder.Entity<BirimMaliyet>().Property(b => b.Malzeme).HasPrecision(18, 2);
            modelBuilder.Entity<BirimMaliyet>().HasData(
                new BirimMaliyet { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), NesneTuru = "Menhol", Iscilik = 2000, Malzeme = 3000 },
                new BirimMaliyet { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), NesneTuru = "Kabin", Iscilik = 4000, Malzeme = 6000 },
                new BirimMaliyet { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), NesneTuru = "Santral", Iscilik = 20000, Malzeme = 30000 },
                new BirimMaliyet { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), NesneTuru = "Fiber", Iscilik = 15, Malzeme = 25 });
            modelBuilder.Entity<Kullanici>().Property(k => k.KullaniciAdi).HasMaxLength(50);
            modelBuilder.Entity<Kullanici>().Property(k => k.Yetki).HasMaxLength(20);
            modelBuilder.Entity<Kullanici>().HasIndex(k => k.KullaniciAdi).IsUnique();
            modelBuilder.Entity<Proje>().Property(p => p.ProjeAdi).HasMaxLength(100);
            modelBuilder.Entity<Proje>().Property(p => p.OlusturanAdi).HasMaxLength(50);
            modelBuilder.Entity<Proje>().Property(p => p.OnaylayanAdi).HasMaxLength(50);
            modelBuilder.Entity<Proje>().Property(p => p.RedNotu).HasMaxLength(500);
            modelBuilder.Entity<Proje>().Property(p => p.Durum).HasMaxLength(20).HasDefaultValue(ProjeDurumlari.Planlama);
            modelBuilder.Entity<Proje>().ToTable(t => t.HasCheckConstraint("CK_Proje_Durum", $"Durum IN ('{ProjeDurumlari.Planlama}', '{ProjeDurumlari.OnayBekliyor}', '{ProjeDurumlari.Onaylandi}')"));
            modelBuilder.Entity<ProjeGecmisi>().HasIndex(g => g.ProjeId);
            modelBuilder.Entity<ProjeGecmisi>().Property(g => g.ProjeAdi).HasMaxLength(100);
            modelBuilder.Entity<ProjeGecmisi>().Property(g => g.Islem).HasMaxLength(30);
            modelBuilder.Entity<ProjeGecmisi>().Property(g => g.KullaniciAdi).HasMaxLength(50);
            modelBuilder.Entity<ProjeGecmisi>().Property(g => g.Not).HasMaxLength(500);
            // SadeceOnaylilar false ise (yonetici, duzenleme, onaylayici) filtre etkisizdir.
            // True ise (goruntuleme) sadece onayli projeler ve onlara ait kayitlar gelir.
            modelBuilder.Entity<Proje>().HasQueryFilter(p =>
                !SadeceOnaylilar || p.Durum == ProjeDurumlari.Onaylandi);
            modelBuilder.Entity<NetworkElement>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));

            modelBuilder.Entity<Santral>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));

            modelBuilder.Entity<Konut>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));

            modelBuilder.Entity<Fiber>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));

            modelBuilder.Entity<ProjeGecmisi>().HasQueryFilter(x =>
                !SadeceOnaylilar || Projeler.Any(p => p.Id == x.ProjeId && p.Durum == ProjeDurumlari.Onaylandi));

        }
    }
}
