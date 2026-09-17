using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebMap.Services
{
    // ProjeKilitliException -> 409 Conflict. Govde duz bir JSON metni oldugu icin
    // map.js'teki sunucuHatasi mesaji dogrudan bildirim olarak gosterir.
    // Diger hatalara dokunmaz; onlar normal hata akisina devam eder.
    public class ProjeKilitliFiltresi : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not ProjeKilitliException hata) return;
            context.Result = new ConflictObjectResult(hata.Message);
            context.ExceptionHandled = true;
        }
    }
}
