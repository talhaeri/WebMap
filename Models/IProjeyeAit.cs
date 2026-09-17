namespace WebMap.Models
{
    // Bir projenin ICINDEKI nesneler: Menhol, Kabin, Santral, Konut, Fiber.
    // Proje'nin kendisi ve ProjeGecmisi bu arayuzu UYGULAMAZ (bkz. ProjeKilidiInterceptor).
    public interface IProjeyeAit
    {
        Guid ProjeId { get; }
    }
}