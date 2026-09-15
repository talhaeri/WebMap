namespace WebMap.Models
{
    public class Kullanici
    {
        public Guid Id { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string ParolaHash   { get; set; } = "";
        public string Yetki { get; set; } = Yetkiler.Goruntuleme;
    }

    public static class Yetkiler
    {
        public const string Goruntuleme = "goruntuleme";
        public const string Duzenleme = "duzenleme";
        // Kullanici yonetimi icin ucuncu yetki: yoksa kullanicilar sayfasina
        // kimin girebilecegini soyleyemeyiz.
        public const string Yonetici = "yonetici";

        // [Authorize(Roles = ...)] derleme zamani sabit ister;
        // sabitlerin birlesimi de sabittir.
        public const string Duzenleyebilir = Yonetici + "," + Duzenleme;
    }
}
