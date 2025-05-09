using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

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

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    return Forbid();
                }
                var user = _userManager.GetUserAsync(User).Result;
                var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);

                var cart = _context.Carts
                    .Where(c => c.CustomerId == customer.CustomerId)
                    .FirstOrDefault();

                if (cart == null)
                {
                    return View("Index");
                }

                // Lấy chi tiết giỏ hàng
                var cartDetails = _context.CartDetails
                    .Where(cd => cd.CartId == cart.CartId)
                    .Include(cd => cd.ProductDetail)
                    .Include(cd => cd.ProductDetail.Color)
                    .Include(cd => cd.ProductDetail.Color.Product)
                    .ThenInclude(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                    .ToList();

                return View(cartDetails);
            }
            else
            {
                // Nếu người dùng không đăng nhập, trả về view hiển thị giỏ hàng từ localStorage
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string size, int quantity, string color)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);

            // Tìm hoặc tạo giỏ hàng
            var cart = _context.Carts.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = customer.CustomerId,
                    CartDetails = new List<CartDetail>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Lấy thông tin ProductDetail dựa trên ProductId, Size và Color
            var productDetail = _context.ProductDetails
                .Include(pd => pd.Color)
                .FirstOrDefault(pd => pd.Color.ProductId == productId && pd.Size == size && pd.Color.Name == color);

            if (productDetail == null)
            {
                return BadRequest("Không tìm thấy thông tin sản phẩm.");
            }

            // Kiểm tra nếu sản phẩm đã tồn tại trong giỏ hàng
            var existingCartDetail = _context.CartDetails
                .FirstOrDefault(cd => cd.CartId == cart.CartId && cd.ProductDetailId == productDetail.ProductDetailId);

            if (existingCartDetail != null)
            {
                // Cập nhật số lượng nếu sản phẩm đã tồn tại
                existingCartDetail.Quantity += quantity;
            }
            else
            {
                // Thêm sản phẩm mới vào giỏ hàng
                var cartDetail = new CartDetail
                {
                    CartId = cart.CartId,
                    ProductDetailId = productDetail.ProductDetailId,
                    Quantity = quantity
                };
                _context.CartDetails.Add(cartDetail);
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartDetailId)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);
            if (customer == null)
            {
                return BadRequest("Không tìm thấy thông tin khách hàng.");
            }

            var cart = _context.Carts.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (cart == null)
            {
                return RedirectToAction("Index");
            }

            var cartDetail = _context.CartDetails.FirstOrDefault(cd =>
                cd.CartDetailId == cartDetailId && cd.CartId == cart.CartId
            );

            if (cartDetail != null)
            {
                _context.CartDetails.Remove(cartDetail);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);
            if (customer == null)
            {
                return BadRequest("Không tìm thấy thông tin khách hàng.");
            }
            var cart = _context.Carts.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (cart != null)
            {
                var cartDetails = _context.CartDetails.Where(cd => cd.CartId == cart.CartId).ToList();

                _context.CartDetails.RemoveRange(cartDetails);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Cart/UpdateQuantity")]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            if (request == null || request.ProductId <= 0 || string.IsNullOrEmpty(request.Size) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var cartItem = _context.CartDetails
                .Include(cd => cd.ProductDetail)
                .ThenInclude(pd => pd.Color)
                .FirstOrDefault(c => c.ProductDetail.ProductDetailId == request.ProductId
                                     && c.ProductDetail.Size == request.Size);

            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            cartItem.Quantity = request.Quantity;
            _context.SaveChanges();

            return Ok(new { message = "Quantity updated successfully" });
        }

        public class UpdateQuantityRequest
        {
            public int ProductId { get; set; }
            public string Size { get; set; }
            public int Quantity { get; set; }
        }
    }

}
