namespace WebMap.Services
{
    // Kilitli projedeki bir nesneye yazma girisimi (bkz. ProjeKilidiInterceptor).
    // ProjeKilitliFiltresi bunu 409 Conflict yanitina cevirir; mesaj kullaniciya gosterilir.
    public class ProjeKilitliException(string mesaj) : Exception(mesaj);
}
