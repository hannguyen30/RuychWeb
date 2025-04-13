using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using RuychWeb.Models.ViewModels;
using RuychWeb.Models.Vnpay;
using RuychWeb.Repository;
using RuychWeb.Repository.Services.Vnpay;

namespace RuychWeb.Controllers
{
    public class UserOrderController : Controller
    {
        private readonly UserManager<Account> _userManager;
        private readonly DataContext _context;
        private readonly ILogger<UserOrderController> _logger;
        private readonly IVnPayService _vnPayService;
        public UserOrderController(UserManager<Account> userManager, DataContext context, ILogger<UserOrderController> logger, IVnPayService vnPayService)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _vnPayService = vnPayService;
        }
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccountId == user.Id);

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

            // Get the shipping options
            var shippingList = await _context.Shippings.ToListAsync();
            ViewBag.Shippings = shippingList;

            // Get the cart details and include related tables
            var cartDetails = await _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .Include(cd => cd.ProductDetail)
                    .ThenInclude(pd => pd.Color)
                        .ThenInclude(c => c.Product)
                            .ThenInclude(p => p.SaleDetails)
                                .ThenInclude(sd => sd.Sale)
                .ToListAsync();

            var orderViewModel = new OrderViewModel
            {
                Name = customer.Name,
                Phone = customer.Phone,
                Address = customer.Address,
                OrderDetails = cartDetails.Select(cd => new OrderDetailViewModel
                {
                    ProductDetailId = cd.ProductDetailId,
                    Quantity = cd.Quantity,
                    Price = cd.ProductDetail?.Color?.Product?.Price ?? 0,
                    ProductName = cd.ProductDetail?.Color?.Product?.Name ?? "Unknown",
                    ColorName = cd.ProductDetail?.Color?.Name ?? "Unknown",
                    Size = cd.ProductDetail?.Size ?? "Unknown",
                    Discount = cd.ProductDetail?.Color?.Product?.SaleDetails
                        .FirstOrDefault()?.Sale?.Discount ?? 0
                }).ToList()
            };

            return View(orderViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(OrderViewModel model, decimal Total)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == user.Id);
            if (customer == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (cart == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            var cartDetails = await _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .Include(cd => cd.ProductDetail)
                .ThenInclude(pd => pd.Color)
                .ThenInclude(c => c.Product)
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            // Tạo đơn hàng và chi tiết đơn hàng
            var order = new Order
            {
                Name = model.Name,
                Phone = model.Phone,
                Address = $"{model.Address}, {model.Phuong}, {model.Quan}, {model.Tinh}",
                CreatedDate = DateTime.Now,
                PaymentMethod = model.PaymentMethod,
                CancelReason = "",
                CarrierName = "Giao hàng tiết kiệm",
                PaymentStatus = "Chưa thanh toán",
                OrderStatus = model.PaymentMethod == "Online" ? "Chờ thanh toán" : "Đang xử lý",  // Đơn hàng online sẽ có trạng thái 'Chờ thanh toán'
                TotalFee = Total,
                CustomerId = customer.CustomerId,
                EmployeeId = 1 // ID nhân viên giao hàng, có thể thay đổi theo logic của bạn
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo OrderDetail và trừ kho sản phẩm ngay khi tạo đơn hàng
            foreach (var cartDetail in cartDetails)
            {
                var orderDetail = new OrderDetail
                {
                    Quantity = cartDetail.Quantity,
                    Price = cartDetail.ProductDetail.Color.Product.Price,
                    OrderId = order.OrderId,
                    ProductDetailId = cartDetail.ProductDetailId
                };

                _context.OrderDetails.Add(orderDetail);

                // Trừ số lượng trong kho sản phẩm
                cartDetail.ProductDetail.Quantity -= cartDetail.Quantity;
                _logger.LogInformation("Trừ kho sản phẩm ID: {ProductDetailId}, SL: {Quantity}", cartDetail.ProductDetailId, cartDetail.Quantity);
            }

            // Xóa CartDetails sau khi tạo đơn hàng
            _context.CartDetails.RemoveRange(cartDetails);
            await _context.SaveChangesAsync();

            // Tiến hành thanh toán cho đơn hàng nếu là thanh toán online
            if (model.PaymentMethod == "Online")
            {
                var paymentInfo = new PaymentInformationModel
                {
                    Amount = Total,
                    OrderDescription = "Thanh toán đơn hàng tại Ruych",
                    Name = model.Name,
                    OrderType = "other",
                    OrderId = order.OrderId
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                return Redirect(paymentUrl);
            }

            // Nếu thanh toán COD, trực tiếp chuyển đến trang Checkout
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            _logger.LogInformation("VNPay callback received. Response: {@response}", response);

            if (response.Success)
            {
                var orderId = response.OrderId;
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order != null && order.PaymentStatus == "Chưa thanh toán")
                {
                    _logger.LogInformation("Cập nhật trạng thái đơn hàng ID {OrderId}", orderId);

                    order.PaymentStatus = "Đã thanh toán";
                    order.PaymentDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["PaymentSuccess"] = true;
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy đơn hàng hoặc đã thanh toán. OrderId: {OrderId}", orderId);
                }
            }
            else
            {
                _logger.LogWarning("Thanh toán thất bại hoặc bị hủy từ VNPay");
            }

            TempData["PaymentResponse"] = JsonConvert.SerializeObject(response);

            // Kiểm tra người dùng đăng nhập hay chưa để điều hướng đúng trang
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Checkout");
            }
            else
            {
                return RedirectToAction("LocalCheckout");
            }
        }



        [HttpPost]
        public IActionResult LocalCheckout([FromBody] List<GuestCartViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return BadRequest("Giỏ hàng không hợp lệ.");

            var orderVM = new OrderViewModel
            {
                // Các thông tin khác như tên, địa chỉ, phương thức thanh toán sẽ được lấy từ form
                OrderDetails = cartItems.Select(item => new OrderDetailViewModel
                {
                    ProductDetailId = item.ProductId, // Giả sử bạn lấy ProductId từ GuestCartViewModel
                    Quantity = item.Quantity,
                    Price = item.Price, // Hoặc bạn có thể lấy Price nếu không có DiscountedPrice
                    ProductName = item.ProductName,
                    ColorName = item.Color,
                    Size = item.Size,
                    Discount = item.Discount,
                }).ToList()
            };
            return View("LocalCheckout", orderVM);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderViewModel model, decimal Total, List<OrderDetailViewModel> list)
        {
            var order = new Order
            {
                Name = model.Name,
                Phone = model.Phone,
                Address = $"{model.Address}, {model.Phuong}, {model.Quan}, {model.Tinh}",
                CreatedDate = DateTime.Now,
                PaymentMethod = model.PaymentMethod,
                CancelReason = "",
                CarrierName = "Giao hàng tiết kiệm",
                PaymentStatus = model.PaymentMethod == "Online" ? "Chưa thanh toán" : "Chưa thanh toán",
                OrderStatus = model.PaymentMethod == "Online" ? "Chờ xác nhận" : "Đang xử lý",
                TotalFee = Total,
                CustomerId = null,
                EmployeeId = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in list)
            {
                var orderDetail = new OrderDetail
                {
                    Quantity = item.Quantity,
                    Price = item.Price,
                    OrderId = order.OrderId,
                    ProductDetailId = item.ProductDetailId
                };

                _context.OrderDetails.Add(orderDetail);

                // Trừ kho
                var productDetail = await _context.ProductDetails.FindAsync(item.ProductDetailId);
                if (productDetail != null)
                {
                    productDetail.Quantity -= item.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            // Xóa cart localStorage thông qua script trong View
            TempData["ClearCart"] = true;

            if (model.PaymentMethod == "Online")
            {
                var paymentInfo = new PaymentInformationModel
                {
                    Amount = Total,
                    OrderDescription = "thanh toán đơn hàng tại Ruych",
                    Name = model.Name,
                    OrderType = "other",
                    OrderId = order.OrderId
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                return Redirect(paymentUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
