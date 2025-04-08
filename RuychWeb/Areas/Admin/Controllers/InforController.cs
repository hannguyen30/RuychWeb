using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using RuychWeb.Models.Domain;
using System.Linq;
using System.Threading.Tasks;
using RuychWeb.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InforController : Controller
    {
        private readonly UserManager<Account> _userManager;
        private DataContext _dataContext;
        public InforController(UserManager<Account> userManager,DataContext dataContext)
        {
            _userManager = userManager;
            this._dataContext = dataContext;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 4)
        {
            var totalAccounts = _userManager.Users.Count();
            var totalPages = (int)Math.Ceiling(totalAccounts / (double)pageSize);

            // Lấy thông tin người dùng từ bảng Account, Customers, Employees
            var accountUsers = _userManager.Users.ToList();

            var employees = await _dataContext.Employees.ToListAsync();
            var customers = await _dataContext.Customers.ToListAsync();

            // Kết hợp các thông tin vào AccountViewModel
            var allUsers = new List<AccountViewModel>();

            foreach (var u in accountUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(u); // Lấy roles một cách bất đồng bộ

                var accountViewModel = new AccountViewModel
                {
                    Id = u.Id,
                    Name = u.UserName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Role = string.Join(", ", userRoles) // Gộp các vai trò thành chuỗi
                };

                // Kết hợp thông tin từ bảng Employees và Customers
                var employee = employees.FirstOrDefault(e => e.AccountId == u.Id);
                var customer = customers.FirstOrDefault(c => c.AccountId == u.Id);

                if (employee != null)
                {
                    accountViewModel.Name = employee.Name; // Cập nhật tên nhân viên nếu có
                    accountViewModel.Phone = employee.Phone; // Cập nhật số điện thoại nếu có
                }

                if (customer != null)
                {
                    accountViewModel.Name = customer.Name; // Cập nhật tên khách hàng nếu có
                    accountViewModel.Phone = customer.Phone; // Cập nhật số điện thoại nếu có
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

            return View(pagedUsers);
        }


        public IActionResult Create()
        {
            return View();
        }

        // Xử lý tạo tài khoản mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Account
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, "Staff");

                    // Thêm Employee
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

                    TempData["SuccessMessage"] = "Account & Employee created successfully!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            string name = "";
            string email = "";
            string phone = "";

            if (role == "Customer")
            {
                var customer = await _dataContext.Customers.FirstOrDefaultAsync(c => c.AccountId == id);
                if (customer != null)
                {
                    name = customer.Name;
                    email = customer.Email;
                    phone = customer.Phone;
                }
            }
            else if (role == "Staff")
            {
                var employee = await _dataContext.Employees.FirstOrDefaultAsync(e => e.AccountId == id);
                if (employee != null)
                {
                    name = employee.Name;
                    email = employee.Email;
                    phone = employee.Phone;
                }
            }

            var viewModel = new AccountViewModel
            {
                Id = id,
                Name = name,
                Email = email,
                Phone = phone,
                Role = role
            };

            return View(viewModel);
        }

        [HttpPost]
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
                }
            }

            await _dataContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

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


    }
}
