using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.ViewModels;
using RuychWeb.Repository.Services;

namespace RuychWeb.Controllers
{
    public class CartController : Controller
    {
        private static List<CartViewModel> CartItems = new List<CartViewModel>();

        [HttpPost]
        public IActionResult AddToCart(CartViewModel model)
        {
            CartItems.Add(model);
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View(CartItems);
        }

    }
}