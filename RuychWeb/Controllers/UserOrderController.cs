using Microsoft.AspNetCore.Authorization;
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
            if (TempData["PError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Phuong), TempData["PError"].ToString());
            }
            if (TempData["QError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Quan), TempData["QError"].ToString());
            }
            if (TempData["TError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Tinh), TempData["TError"].ToString());
            }
            if (TempData["NameError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Name), TempData["NameError"].ToString());
            }
            if (TempData["PhoneError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Phone), TempData["PhoneError"].ToString());
            }
            if (TempData["AddressError"] != null)
            {
                ModelState.AddModelError(nameof(OrderViewModel.Address), TempData["AddressError"].ToString());
            }

            var user = await _userManager.GetUserAsync(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccountId == user.Id);

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

            var shippingList = await _context.Shippings.ToListAsync();
            ViewBag.Shippings = shippingList;

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
                    Discount = cd.ProductDetail?.Color?.Product?.SaleDetails.
                        Select(sd => (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0).FirstOrDefault()
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

            if (model == null ||
                  model.Tinh == "0" || model.Quan == "0" || model.Phuong == "0" ||
                  string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Phone) ||
                   !model.Phone.All(char.IsDigit) || model.Phone.Length < 10 || string.IsNullOrEmpty(model.Address))
            {
                if (model.Phuong == "0")
                {
                    TempData["PError"] = "Vui lòng chọn Phường/Xã.";
                }
                if (model.Quan == "0")
                {
                    TempData["QError"] = "Vui lòng chọn Quận/Huyện.";
                }
                if (model.Tinh == "0")
                {
                    TempData["TError"] = "Vui lòng chọn Tỉnh/Thành Phố.";
                }
                if (string.IsNullOrEmpty(model.Name))
                {
                    TempData["NameError"] = "Vui lòng nhập họ tên.";
                }
                if (string.IsNullOrEmpty(model.Phone))
                {
                    TempData["PhoneError"] = "Vui lòng nhập số điện thoại.";
                }
                else if (!model.Phone.All(char.IsDigit) || model.Phone.Length < 10)
                {
                    TempData["PhoneError"] = "Số điện thoại không hợp lệ (Cần 10 chữ số).";
                }
                if (string.IsNullOrEmpty(model.Address))
                {
                    TempData["AddressError"] = "Vui lòng nhập địa chỉ.";
                }

                // Quay lại trang Checkout
                return RedirectToAction("Checkout");
            }


            var order = new Order
            {
                Name = model.Name,
                Phone = model.Phone,
                Address = $"{model.Address}, {model.Phuong}, {model.Quan}, {model.Tinh}",
                CreatedDate = DateTime.Now,
                CompletedDate = null,
                PaymentMethod = model.PaymentMethod,
                CancelReason = "",
                CarrierName = "Giao hàng tiết kiệm",
                PaymentStatus = "Chưa thanh toán",
                OrderStatus = model.PaymentMethod == "Online" ? "Chờ xác nhận" : "Chờ xác nhận",
                TotalFee = Total,
                CustomerId = customer.CustomerId,
                EmployeeId = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo OrderDetail và trừ kho sản phẩm ngay khi tạo đơn hàng
            foreach (var cartDetail in cartDetails)
            {
                var orderDetail = new OrderDetail
                {
                    Quantity = cartDetail.Quantity,
                    Price = (decimal)cartDetail.ProductDetail.Color.Product.Price,
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
            TempData["OrderId"] = order.OrderId;
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("Index", "Home", new { success = "true" });
        }

        // Trang dành cho khách hàng chưa đăng nhập
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

        // Đặt hàng cho khách hàng chưa đăng nhập
        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderViewModel model, decimal Total)
        {
            foreach (var item in model.OrderDetails)
            {
                _logger.LogInformation($"OrderDetailViewModel => ProductDetailId: {item.ProductDetailId}, Quantity: {item.Quantity}, Price: {item.Price}");
            }

            if (model == null || model.Tinh == "0" || model.Quan == "0" || model.Phuong == "0")
            {
                if (model.Phuong == "0")
                {
                    ModelState.AddModelError(nameof(model.Phuong), "Vui lòng chọn Phường/Xã.");
                }
                if (model.Quan == "0")
                {
                    ModelState.AddModelError(nameof(model.Quan), "Vui lòng chọn Quận/Huyện.");
                }
                if (model.Tinh == "0")
                {
                    ModelState.AddModelError(nameof(model.Tinh), "Vui lòng chọn Tỉnh/Thành Phố.");
                }

                return View("LocalCheckout", model);  // Quay lại trang hiện tại và thông báo lỗi
            }

            var order = new Order
            {
                Name = model.Name,
                Phone = model.Phone,
                Address = $"{model.Address}, {model.Phuong}, {model.Quan}, {model.Tinh}",
                CreatedDate = DateTime.Now,
                CompletedDate = null,
                PaymentMethod = model.PaymentMethod,
                CancelReason = "",
                CarrierName = "Giao hàng tiết kiệm",
                PaymentStatus = model.PaymentMethod == "Online" ? "Chưa thanh toán" : "Chưa thanh toán",
                OrderStatus = model.PaymentMethod == "Online" ? "Chờ xác nhận" : "Chờ xác nhận",
                TotalFee = Total,
                CustomerId = null,
                EmployeeId = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Thêm OrderDetail
            foreach (var item in model.OrderDetails)
            {
                var orderDetail = new OrderDetail
                {
                    Quantity = item.Quantity,
                    Price = item.Price,
                    OrderId = order.OrderId,
                    ProductDetailId = item.ProductDetailId
                };

                _context.OrderDetails.Add(orderDetail);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Đã lưu OrderDetails vào database.");

            // Lấy lại OrderDetail
            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderId == order.OrderId)
                .ToListAsync();

            _logger.LogInformation($"Tìm thấy {orderDetails.Count} OrderDetail cho OrderId={order.OrderId}");

            // Trừ kho
            foreach (var od in orderDetails)
            {
                var productDetail = await _context.ProductDetails.FindAsync(od.ProductDetailId);
                if (productDetail != null)
                {
                    int oldQuantity = productDetail.Quantity;
                    productDetail.Quantity -= od.Quantity;
                    if (productDetail.Quantity < 0)
                        productDetail.Quantity = 0;

                    _logger.LogInformation($"Trừ kho: ProductDetailId={od.ProductDetailId}, Số lượng cũ={oldQuantity}, Trừ={od.Quantity}, Mới={productDetail.Quantity}");
                }
                else
                {
                    _logger.LogWarning($"Không tìm thấy ProductDetail với ID: {od.ProductDetailId}");
                }
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("Đã cập nhật số lượng kho.");

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
            TempData["OrderId"] = order.OrderId;
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("Index", "Home", new { success = "true" });
        }

        // Trả về dữ liệu sau khi thanh toán xong
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
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        //Xem lịch sử cho khách hàng đăng nhập
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderHistory()
        {
            List<Order> orders = new List<Order>();

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == user.Id);

                if (customer != null)
                {
                    orders = await _context.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.ProductDetail)
                                .ThenInclude(pd => pd.Color)
                                .ThenInclude(pd => pd.Product)
                                .ThenInclude(p => p.SaleDetails)
                                    .ThenInclude(sd => sd.Sale)
                        .Where(o => o.CustomerId == customer.CustomerId && o.OrderStatus == "Đã hoàn thành")
                        .OrderByDescending(o => o.CreatedDate)
                        .ToListAsync();
                }
            }
            var orderViewModels = orders.Select(o =>
            {
                // Tách địa chỉ thành các phần Address, Ward, District, Province
                var (Address, Ward, District, Province) = SplitAddress(o.Address);

                return new OrderHistoryViewModel
                {
                    OrderId = o.OrderId,
                    Name = o.Name,
                    Phone = o.Phone,
                    CreatedDate = o.CreatedDate,
                    CompletedDate = o.CompletedDate,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    TotalFee = o.TotalFee,
                    // Gán các giá trị đã tách vào model
                    Address = Address,
                    Ward = Ward,
                    District = District,
                    Province = Province,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.ProductDetail.Color.Product.Name,
                        ColorName = od.ProductDetail.Color.Name,
                        Size = od.ProductDetail.Size,
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Discount = od.ProductDetail.Color.Product.SaleDetails.
                            Select(sd => (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0).FirstOrDefault(),
                        ImageUrl = od.ProductDetail.Color.Product.Thumbnail
                    }).ToList()
                };
            }).ToList();

            return View(orderViewModels);

        }

        //Hàm tách dịa chỉ từ chuỗi số nhập từ form
        public static (string Address, string Ward, string District, string Province) SplitAddress(string fullAddress)
        {
            var parts = fullAddress?.Split(',') ?? new string[0];

            string address = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string ward = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            string district = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            string province = parts.Length > 3 ? parts[3].Trim() : string.Empty;

            return (address, ward, district, province);
        }

        //Trang tìm kiếm đơn hàng
        [HttpGet]
        public IActionResult LookupOrder()
        {
            return View(new OrderLookupViewModel());
        }

        // Trả về dữ liệu tìm kiếm đơn hàng
        [HttpPost]
        public async Task<IActionResult> LookupOrder(OrderLookupViewModel model)
        {
            if (!model.OrderId.HasValue || string.IsNullOrEmpty(model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return View(model);
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                                .ThenInclude(p => p.SaleDetails)
                                    .ThenInclude(sd => sd.Sale)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.Phone == model.Phone);

            if (order == null)
            {
                model.NotFound = true;
                return View(model);
            }

            var (address, ward, district, province) = SplitAddress(order.Address);

            model.OrderResult = new OrderHistoryViewModel
            {
                OrderId = order.OrderId,
                Name = order.Name,
                Phone = order.Phone,
                CreatedDate = order.CreatedDate,
                CompletedDate = order.CompletedDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                TotalFee = order.TotalFee,
                Address = address,
                Ward = ward,
                District = district,
                Province = province,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.ProductDetail.Color.Product.Name,
                    ColorName = od.ProductDetail.Color.Name,
                    Size = od.ProductDetail.Size,
                    Quantity = od.Quantity,
                    Price = od.Price,
                    Discount = od.ProductDetail.Color.Product.SaleDetails
                    .Select(sd => (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0).FirstOrDefault(),
                    ImageUrl = od.ProductDetail.Color.Product.Thumbnail
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId, string cancelReason)
        {
            // Find the order by its ID
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            // Check if the order is in a valid cancelable state
            if (order.OrderStatus == "Chờ xác nhận" || order.OrderStatus == "Đã xác nhận")
            {
                order.OrderStatus = "Yêu cầu hủy";
                order.CancelReason = cancelReason;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                return RedirectToAction("LookupOrder");
            }
            else
            {
                return RedirectToAction("LookupOrder");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReceived(int OrderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == OrderId);
            if (order == null || order.OrderStatus != "Giao hàng thành công")
            {
                return NotFound();
            }

            order.OrderStatus = "Đã hoàn thành";
            order.CompletedDate = DateTime.Now;

            if (order.PaymentStatus == "Chưa thanh toán")
            {
                order.PaymentStatus = "Đã thanh toán";
                order.PaymentDate = DateTime.Now;
            }
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xác nhận nhận hàng.";
            return RedirectToAction("LookupOrder");
        }
    }
}
