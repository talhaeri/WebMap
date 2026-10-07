namespace WebMap.Models
{
    // Nesne türü başına sabit birim fiyat: Menhol, Kabin, Santral, Fiber (seed: AppDbContext).
    // Birim fiyatın ölçüsü türe göre değişir (bkz. MaliyetHesaplayici).
    public class BirimMaliyet
    {
        public Guid Id { get; set; }
        public string NesneTuru { get; set; } = "";
        public decimal Iscilik { get; set; }
        public decimal Malzeme { get; set; }
    }
}
