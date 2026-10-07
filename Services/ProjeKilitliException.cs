namespace WebMap.Services
{
    // Kilitli projedeki nesneye yazma girişimi (bkz. ProjeKilidiInterceptor).
    // ProjeKilitliFiltresi bunu 409 yanıtına çevirir; mesaj kullanıcıya gösterilir.
    public class ProjeKilitliException(string mesaj) : Exception(mesaj);
}
