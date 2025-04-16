using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.Domain;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<Account> _userManager;

        public OrderController(DataContext context, UserManager<Account> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var totalOrder = await _context.Orders.CountAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalOrder / (double)pageSize);

            // Đảm bảo pageNumber không vượt quá tổng số trang
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)  // Bỏ qua các sản phẩm của các trang trước
                .Take(pageSize)
                .ToListAsync();
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            return View(orders);
        }

        [HttpGet("Admin/Order/Details/{orderId}")]
        public IActionResult Details(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                            .ThenInclude(c => c.SaleDetails)
                            .ThenInclude(c => c.Sale)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null) return NotFound();
            var (address, ward, district, province) = SplitAddress(order.Address);
            var orderViewModel = new OrderManagerViewModel
            {
                OrderId = order.OrderId,
                Name = order.Name,
                Phone = order.Phone,
                Address = address,
                Ward = ward,
                District = district,
                Province = province,
                CreatedDate = order.CreatedDate,
                CompletedDate = order.CompletedDate,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = order.PaymentDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                CancelReason = order.CancelReason,
                CarrierName = order.CarrierName,
                TotalFee = order.TotalFee,
                CustomerId = order.CustomerId,
                EmployeeId = order.EmployeeId,
                Discount = order.OrderDetails
                    .SelectMany(od => od.ProductDetail.Color.Product.SaleDetails)
                    .Where(sd => sd.Sale != null)
                    .Select(sd => sd.Sale.Discount)  // Retrieve the Discount from Sale
                        .FirstOrDefault(),
                OrderDetails = order.OrderDetails.ToList(),
            };

            return View(orderViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userName = User.Identity.Name;

            var currentEmployee = await _userManager.FindByNameAsync(userName);
            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AccountId == currentEmployee.Id);
            if (employee == null)
            {

                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var viewModel = new OrderEditViewModel
            {
                OrderId = order.OrderId,
                OrderStatus = order.OrderStatus,
                CancelReason = order.CancelReason,
                CompletedDate = order.CompletedDate,
                EmployeeId = employee.EmployeeId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userName = User.Identity.Name;

                var currentEmployee = await _userManager.FindByNameAsync(userName);
                if (currentEmployee == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

                if (order == null)
                {
                    return NotFound();
                }

                // Cập nhật trạng thái và lý do hủy
                order.OrderStatus = model.OrderStatus;
                order.CancelReason = model.CancelReason;

                order.EmployeeId = (int)model.EmployeeId;

                // Nếu trạng thái là "Đã hoàn thành", set ngày hoàn thành
                if (model.OrderStatus == "Đã hoàn thành" && !order.CompletedDate.HasValue)
                {
                    order.CompletedDate = DateTime.Now; // Hoặc model.CompletedDate nếu muốn sử dụng ngày từ form
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { orderId = order.OrderId });
            }

            return View(model);
        }



        [HttpPost]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        public static (string Address, string Ward, string District, string Province) SplitAddress(string fullAddress)
        {
            // Giả sử bạn có địa chỉ theo định dạng: "Số nhà, Đường, Phường, Quận, Tỉnh"
            var parts = fullAddress?.Split(',') ?? new string[0];

            // Tạo giá trị mặc định nếu không có đủ phần
            string address = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string ward = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            string district = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            string province = parts.Length > 3 ? parts[3].Trim() : string.Empty;

            return (address, ward, district, province);
        }


        [HttpGet]
        public async Task<IActionResult> Search(string keyword = "")
        {
            keyword = keyword.ToLower().Trim();

            var orders = await _context.Orders
                .Where(o =>
                    o.Name.ToLower().Contains(keyword) ||
                    o.Phone.Contains(keyword)
                )
                .ToListAsync();

            return Json(orders);
        }
    }
}
