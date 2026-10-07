using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebMap.Models;

namespace WebMap.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        // Hata sayfası giriş yapmamış kullanıcıya da açık olmalı
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
