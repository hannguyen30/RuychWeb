using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using System.Linq;
using System.Threading.Tasks;
using RuychWeb.Repository;
using Microsoft.EntityFrameworkCore;

namespace RuychWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly UserManager<Account> _userManager;
        private readonly DataContext _context;

        public CartController(UserManager<Account> userManager, DataContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Hiển thị giỏ hàng của người dùng đang đăng nhập
        public async Task<IActionResult> Index()
        {
            // Lấy thông tin tài khoản người dùng đang đăng nhập
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang đăng nhập nếu không có tài khoản
            }

            // Lấy giỏ hàng của người dùng từ CSDL
            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);

            if (customer == null)
            {
                return View("EmptyCart"); // Trả về trang thông báo nếu không tìm thấy khách hàng
            }

            var cart = _context.Carts
                .Where(c => c.CustomerId == customer.CustomerId)
                .FirstOrDefault();

            if (cart == null)
            {
                return View("EmptyCart"); // Trả về trang thông báo nếu không có giỏ hàng
            }

            // Lấy chi tiết giỏ hàng (bao gồm Product, Color, và Size)
            var cartDetails = _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .Include(cd => cd.ProductDetail)  // Include ProductDetail entity
                .Include(cd => cd.ProductDetail.Color)
                .Include(cd => cd.ProductDetail.Color.Product)// Include the related Product
                .ToList();

            // Truyền dữ liệu giỏ hàng vào view
            return View(cartDetails);
        }


    }
}
