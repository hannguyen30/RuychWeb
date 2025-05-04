using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SupplierController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(DataContext context, ILogger<SupplierController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchTerm, int pageNumber = 1, int pageSize = 4)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.Name.Contains(searchTerm) || s.Phone.Contains(searchTerm) || s.Address.Contains(searchTerm));
            }

            var totalSuppliers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalSuppliers / (double)pageSize);

            var suppliers = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;

            // Nếu yêu cầu là AJAX (tìm kiếm trực tiếp)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { suppliers, pageNumber, totalPages });
            }

            return View(suppliers);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(p => p.Name == supplier.Name);
                if (existingSupplier != null)
                {
                    // Nếu sản phẩm đã tồn tại, hiển thị thông báo lỗi
                    ModelState.AddModelError("Name", "Nhà cung cấp với tên này đã tồn tại.");
                    return View(supplier);
                }
                if (supplier.Receipts == null)
                {
                    supplier.Receipts = new List<Receipt>();
                }

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {

            _context.Update(supplier);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thay đổi thông tin nhà cung cấp thành công!";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Xóa nhà cung cấp thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
