using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.Domain;
using RuychWeb.Repository;
using System.Text.RegularExpressions;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InforController : Controller
    {
        private readonly UserManager<Account> _userManager;
        private DataContext _dataContext;
        public InforController(UserManager<Account> userManager, DataContext dataContext)
        {
            _userManager = userManager;
            this._dataContext = dataContext;
        }
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var totalAccounts = _userManager.Users.Count(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            var totalPages = (int)Math.Ceiling(totalAccounts / (double)pageSize);

            // Lấy thông tin người dùng từ bảng Account, Customers, Employees
            var accountUsers = _userManager.Users
                                           .Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm)) // Tìm kiếm theo tên hoặc email
                                           .ToList();

            var employees = await _dataContext.Employees.ToListAsync();
            var customers = await _dataContext.Customers.ToListAsync();

            // Kết hợp các thông tin vào AccountViewModel
            var allUsers = new List<AccountViewModel>();

            foreach (var u in accountUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(u);

                var accountViewModel = new AccountViewModel
                {
                    Id = u.Id,
                    Name = u.UserName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Role = string.Join(", ", userRoles)
                };

                // Kết hợp thông tin từ bảng Employees và Customers
                var employee = employees.FirstOrDefault(e => e.AccountId == u.Id);
                var customer = customers.FirstOrDefault(c => c.AccountId == u.Id);

                if (employee != null)
                {
                    accountViewModel.Name = employee.Name;
                    accountViewModel.Phone = employee.Phone;
                }

                if (customer != null)
                {
                    accountViewModel.Name = customer.Name;
                    accountViewModel.Phone = customer.Phone;
                }

                allUsers.Add(accountViewModel);
            }

            // Phân trang
            var pagedUsers = allUsers.Skip((pageNumber - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToList();

            // Truyền thông tin phân trang và danh sách người dùng vào View
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm; // Truyền giá trị tìm kiếm

            return View(pagedUsers);
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check email tồn tại
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                    return View(model);
                }

                // Check định dạng email cụ thể
                var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@gmail\.com$");
                if (!emailRegex.IsMatch(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Email không hợp lệ. Vui lòng dùng địa chỉ @gmail.com.");
                    return View(model);
                }

                var username = model.Email.Split('@')[0];

                var user = new Account
                {
                    UserName = username,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");

                    var employee = new Employee
                    {
                        Name = "",
                        Phone = "",
                        Email = model.Email,
                        Birthday = null,
                        AccountId = user.Id
                    };

                    await _dataContext.Employees.AddAsync(employee);
                    await _dataContext.SaveChangesAsync();

                    TempData["Success"] = "Thêm thành công!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            string name = user.UserName;
            string email = user.Email;
            string phone = user.PhoneNumber;
            string address = "";
            DateTime? birthday = null;

            // Lấy tên từ bảng Customer hoặc Employee, tùy thuộc vào vai trò của người dùng
            if (role == "Customer")
            {
                var customer = await _dataContext.Customers.FirstOrDefaultAsync(c => c.AccountId == id);
                if (customer != null)
                {
                    name = customer.Name;
                    phone = customer.Phone;
                    address = customer.Address;
                }
            }
            else if (role == "Staff")
            {
                var employee = await _dataContext.Employees.FirstOrDefaultAsync(e => e.AccountId == id);
                if (employee != null)
                {
                    name = employee.Name;
                    phone = employee.Phone;
                    birthday = employee.Birthday;
                }
            }

            var viewModel = new AccountViewModel
            {
                Id = id,
                Name = name,
                Email = email,
                Phone = phone,
                Role = role,
                Address = address,
                Birthday = birthday
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(AccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (role == "Customer")
            {
                var customer = await _dataContext.Customers.FirstOrDefaultAsync(c => c.AccountId == user.Id);
                if (customer != null)
                {
                    customer.Name = model.Name;
                    customer.Email = model.Email;
                    customer.Phone = model.Phone;
                    customer.Address = model.Address;  // Cập nhật địa chỉ cho Customer
                }
            }
            else if (role == "Staff")
            {
                var employee = await _dataContext.Employees.FirstOrDefaultAsync(e => e.AccountId == user.Id);
                if (employee != null)
                {
                    employee.Name = model.Name;
                    employee.Email = model.Email;
                    employee.Phone = model.Phone;
                    employee.Birthday = model.Birthday;  // Cập nhật ngày sinh cho Staff
                }
            }

            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Thay đổi thông tin thành công!";
            return RedirectToAction("Index");
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy vai trò hiện tại của người dùng
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            // Xóa khỏi bảng Customers nếu có
            if (role == "Customer")
            {
                var customer = await _dataContext.Customers.FirstOrDefaultAsync(c => c.AccountId == id);
                if (customer != null)
                {
                    _dataContext.Customers.Remove(customer);
                }
            }
            // Xóa khỏi bảng Employees nếu có
            else if (role == "Staff")
            {
                var employee = await _dataContext.Employees.FirstOrDefaultAsync(e => e.AccountId == id);
                if (employee != null)
                {
                    _dataContext.Employees.Remove(employee);
                }
            }

            // Xóa khỏi vai trò
            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }

            // Xóa user khỏi Identity
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi khi xóa tài khoản.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> SearchAccounts(string SearchString, int pageNumber = 1, int pageSize = 10)
        {
            var totalAccounts = _userManager.Users.Count(u => u.UserName.Contains(SearchString) || u.Email.Contains(SearchString));
            var totalPages = (int)Math.Ceiling(totalAccounts / (double)pageSize);

            var accountUsers = _userManager.Users
                                           .Where(u => u.UserName.Contains(SearchString) || u.Email.Contains(SearchString))
                                           .Skip((pageNumber - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToList();

            var employees = await _dataContext.Employees.ToListAsync();
            var customers = await _dataContext.Customers.ToListAsync();

            var allUsers = new List<AccountViewModel>();

            foreach (var u in accountUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(u);

                var accountViewModel = new AccountViewModel
                {
                    Id = u.Id,
                    Name = u.UserName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Role = string.Join(", ", userRoles)
                };

                var employee = employees.FirstOrDefault(e => e.AccountId == u.Id);
                var customer = customers.FirstOrDefault(c => c.AccountId == u.Id);

                if (employee != null)
                {
                    accountViewModel.Name = employee.Name;
                    accountViewModel.Phone = employee.Phone;
                }

                if (customer != null)
                {
                    accountViewModel.Name = customer.Name;
                    accountViewModel.Phone = customer.Phone;
                }

                allUsers.Add(accountViewModel);
            }

            return Json(new { users = allUsers, totalPages });
        }

    }
}
