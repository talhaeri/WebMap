using Microsoft.EntityFrameworkCore;
using WebMap.Models;

namespace WebMap.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<NetworkElement> NetworkElements => Set<NetworkElement>();
        public DbSet<Kabin> Kabinler => Set<Kabin>();
        public DbSet<Menhol> Menholler => Set<Menhol>();
        public DbSet<Santral> Santraller => Set<Santral>();
        public DbSet<Konut> Konutlar => Set<Konut>();
        public DbSet<Fiber> Fiberler => Set<Fiber>();
        public DbSet<Proje> Projeler => Set<Proje>();
        public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
        public DbSet<BirimMaliyet> BirimMaliyetler => Set<BirimMaliyet>();

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
        }
    }
}
