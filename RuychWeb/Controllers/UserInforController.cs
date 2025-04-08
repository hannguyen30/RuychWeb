using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.Domain;
using RuychWeb.Repository;
using System.Security.Claims;

namespace RuychWeb.Controllers
{
    public class UserInforController : Controller
    {
        private readonly ILogger<UserInforController> _logger;
        private readonly DataContext _dataContext;

        public UserInforController(ILogger<UserInforController> logger, DataContext context)
        {
            _logger = logger;
            _dataContext = context;
        }

        // Hiển thị trang MyProfile
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            // Lấy ID của người dùng đang đăng nhập
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (accountId == null)
                return RedirectToAction("Login", "Account");

            // Tìm customer theo accountId
            var customer = await _dataContext.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.AccountId == accountId);

            // Trả về view và customer đã bao gồm các trường nullable
            return View(customer);
        }
        [HttpPost]
        [Authorize]  // Đảm bảo rằng người dùng đã đăng nhập
        public async Task<IActionResult> MyProfile(Customer model)
        {
            // Lấy ID của người dùng đang đăng nhập
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (accountId == null)
                return RedirectToAction("Login", "Account");

            // Tìm customer theo accountId
            var customer = await _dataContext.Customers
                .FirstOrDefaultAsync(c => c.AccountId == accountId);

            if (customer == null)
            {
                return NotFound(); // Nếu không tìm thấy khách hàng, trả về lỗi
            }

            // Cập nhật thông tin khách hàng từ model
            customer.Name = model.Name;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;

            // Lưu thay đổi vào cơ sở dữ liệu
            _dataContext.Customers.Update(customer);
            await _dataContext.SaveChangesAsync();

            // Sau khi lưu thành công, chuyển hướng lại trang thông tin cá nhân
            return RedirectToAction("MyProfile");
        }

    }

}
