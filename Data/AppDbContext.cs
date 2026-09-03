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
        public DbSet<Alan> Alanlar => Set<Alan>();
        public DbSet<Konut> Konutlar => Set<Konut>();
        public DbSet<Ticari> Ticariler => Set<Ticari>();
        public DbSet<Fiber> Fiberler => Set<Fiber>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ----- TPT (Table-Per-Type) -----
            // Her tip kendi tablosuna yazilir. Alt tip tablolarinin Id'si,
            // taban tablonun Id'sine FK'dir (ON DELETE CASCADE). Taban tablo
            // ortak sutunlari (Id, Konum) tutar; Fiber ileride buraya baglanabilir.

            modelBuilder.Entity<NetworkElement>().UseTptMappingStrategy();
            modelBuilder.Entity<NetworkElement>().ToTable("NetworkElements");
            modelBuilder.Entity<Kabin>().ToTable("Kabinler");
            modelBuilder.Entity<Menhol>().ToTable("Menholler");

            modelBuilder.Entity<Alan>().UseTptMappingStrategy();
            modelBuilder.Entity<Alan>().ToTable("Alanlar");
            modelBuilder.Entity<Konut>().ToTable("Konutlar");
            modelBuilder.Entity<Ticari>().ToTable("Ticariler");

            // SQL Server'da decimal icin hassasiyet acikca verilmeli (yoksa decimal(18,2) + uyari).
            modelBuilder.Entity<Menhol>().Property(m => m.Derinlik).HasPrecision(9, 2);
        }
    }
}
