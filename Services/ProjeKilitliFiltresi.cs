using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebMap.Services
{
    // ProjeKilitliException -> 409 Conflict. Gövde düz metin olduğu için map.js mesajı doğrudan bildirim olarak gösterir.
    // Başka hatalara dokunmaz.
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
