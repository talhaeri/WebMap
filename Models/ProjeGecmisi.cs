namespace WebMap.Models
{
    // Projenin olay geçmişi. FK yok: proje silinse de kayıt kalır, bu yüzden proje ve kullanıcı adı kopyalanır.
    public class ProjeGecmisi
    {
        public Guid Id { get; set; }
        public Guid ProjeId { get; set; }
        public string ProjeAdi { get; set; } = "";
        public string Islem { get; set; } = "";   // ProjeIslemleri
        public Guid? KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public DateTime Tarih { get; set; }
        public string? Not { get; set; }
        public string? MaliyetJson { get; set; }  // sadece Onayla satırında: onay anındaki maliyet

        public static ProjeGecmisi Yeni(Guid projeId, string projeAdi, string islem, Guid? kullaniciId, string kullaniciAdi,
            string? not = null, string? maliyetJson = null) => new()
        {
            ProjeId = projeId,
            ProjeAdi = projeAdi,
            Islem = islem,
            KullaniciId = kullaniciId,
            KullaniciAdi = kullaniciAdi,
            Tarih = DateTime.UtcNow,
            Not = not,
            MaliyetJson = maliyetJson
        };
    }
}
