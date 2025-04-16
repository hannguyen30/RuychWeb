using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.Domain;
using RuychWeb.Repository;
using System.Security.Claims;

namespace RuychWeb.Controllers
{
    [Authorize(Roles = "Customer")]
    public class UserInforController : Controller
    {
        private readonly ILogger<UserInforController> _logger;
        private readonly DataContext _dataContext;

        public UserInforController(ILogger<UserInforController> logger, DataContext context)
        {
            _logger = logger;
            _dataContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            // Lấy ID của người dùng đang đăng nhập
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (accountId == null)
                return RedirectToAction("Login", "Account");

            var customer = await _dataContext.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.AccountId == accountId);
            return View(customer);
        }

        public async Task<IActionResult> MyProfile(Customer model)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (accountId == null)
                return RedirectToAction("Login", "Account");

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
