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
        public const string Yonetici = "yonetici";
        public const string Onaylayici = "onaylayici";
        public const string Duzenleyebilir = Yonetici + "," + Duzenleme;
        public static readonly string[] TumYetkiler = [Yonetici, Duzenleme, Onaylayici, Goruntuleme];
    }
}
