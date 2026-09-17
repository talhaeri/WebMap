using System.Security.Claims;

namespace WebMap.Services
{
    public class KullaniciBaglami(IHttpContextAccessor erisim)
    {
        ClaimsPrincipal? Kullanici => erisim.HttpContext?.User;
        public string? Rol => Kullanici?.FindFirst(ClaimTypes.Role)?.Value;
        public string Ad => Kullanici?.FindFirst(ClaimTypes.Name)?.Value ?? "";
        // Oturum yoksa null (Guid.Empty degil); bozuk bir deger de hata firlatmaz.
        public Guid? Id => Guid.TryParse(Kullanici?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
