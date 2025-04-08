using Microsoft.AspNetCore.Mvc;

namespace RuychWeb.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
