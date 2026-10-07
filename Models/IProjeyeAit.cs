namespace WebMap.Models
{
    // Bir projenin içindeki nesneler: Menhol, Kabin, Santral, Konut, Fiber.
    // ProjeId bir FK değildir; proje varlığını Ekle uçları denetler.
    // Proje ve ProjeGecmisi bunu uygulamaz, yoksa kilit (ProjeKilidiInterceptor) onları da engellerdi.
    public interface IProjeyeAit
    {
        Guid ProjeId { get; }
    }
}
