using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SaleController : Controller
    {
        private readonly DataContext _context;

        public SaleController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string keyword, int pageNumber = 1, int pageSize = 4)
        {
            keyword = keyword?.Trim();

            var query = _context.Sales.AsQueryable();


            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(s => s.Name.Contains(keyword) || s.Discount.ToString().Contains(keyword));
            }

            var totalSales = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalSales / (double)pageSize);

            var sales = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.Keyword = keyword;

            return View(sales);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Sale sale)
        {
            if (ModelState.IsValid)
            {
                var existingSale = await _context.Sales
                    .FirstOrDefaultAsync(p => p.Name == sale.Name);
                if (existingSale != null)
                {
                    // Nếu sản phẩm đã tồn tại, hiển thị thông báo lỗi
                    ModelState.AddModelError("Name", "Chương trình giảm giá với tên này đã tồn tại.");
                    return View(sale);
                }
                if (sale.SaleDetails == null)
                {
                    sale.SaleDetails = new List<SaleDetail>();
                }

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(sale);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return View(sale);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Sale sale)
        {
            if (id != sale.SaleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(sale);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thay đổi thông tin giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(sale);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null)
            {
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();

            }
            TempData["Success"] = "Xóa giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
