using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.ViewModels;
using RuychWeb.Repository.Services;

namespace RuychWeb.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartViewModel>>("Cart") ?? new List<CartViewModel>();
            return View(cart);
        }
        public IActionResult AddToCart(int productId, string color, string size, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartViewModel>>("Cart") ?? new List<CartViewModel>();

            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId && x.Color == color && x.Size == size);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var product = _context.Products.Find(productId);
                cart.Add(new CartViewModel
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Image = product.Image,
                    Price = product.Price,
                    Color = color,
                    Size = size,
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Index");
        }

    }
}
