using System.Security.Claims;

namespace WebMap.Services
{
    // Oturumdaki kullanıcının bilgileri (giriş çerezindeki talepler)
    public class KullaniciBaglami(IHttpContextAccessor erisim)
    {
        ClaimsPrincipal? Kullanici => erisim.HttpContext?.User;

        public string? Rol => Kullanici?.FindFirst(ClaimTypes.Role)?.Value;
        public string Ad => Kullanici?.FindFirst(ClaimTypes.Name)?.Value ?? "";

        // Oturum yoksa ya da değer bozuksa null (hata fırlatmaz)
        public Guid? Id => Guid.TryParse(Kullanici?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
