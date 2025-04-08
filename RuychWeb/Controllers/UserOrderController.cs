using Microsoft.AspNetCore.Mvc;

namespace RuychWeb.Controllers
{
    public class UserOrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
