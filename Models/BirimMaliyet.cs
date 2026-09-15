namespace WebMap.Models
{
    // Nesne turu basina SABIT birim fiyat (kullanicidan alinmaz, formda gosterilmez).
    // Tam 4 satir: Menhol / Kabin / Santral / Fiber (bkz. AppDbContext seed data).
    // Her tur icin iki kalem: Iscilik + Malzeme. Birim fiyatin OLCUSU ture gore degisir
    // (bkz. MaliyetHesaplayici):
    public class BirimMaliyet
    {
        public Guid Id { get; set; }
        public string NesneTuru { get; set; } = "";
        public decimal Iscilik { get; set; }   // birim iscilik maliyeti
        public decimal Malzeme { get; set; }   // birim malzeme maliyeti
    }
}
