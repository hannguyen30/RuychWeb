using Microsoft.AspNetCore.Mvc;

namespace RuychWeb.Areas.Admin.Controllers
{
    public class SupplierController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
