using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly DataContext _context;

        public CategoryController(DataContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Name.Contains(searchString));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;

            var categories = await query
                .OrderBy(c => c.CategoryId) // Thêm order cho ổn định
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(categories);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingCategory = await _context.Categories
                     .FirstOrDefaultAsync(p => p.Name == model.Name);
                if (existingCategory != null)
                {
                    // Nếu sản phẩm đã tồn tại, hiển thị thông báo lỗi
                    ModelState.AddModelError("Name", "Danh mục với tên này đã tồn tại.");
                    return View(model);
                }
                var category = new Category
                {
                    Name = model.Name
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin chuyên mục
                category.Name = model.Name;

                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thông tin đã được thay đổi thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var allCategories = await _context.Categories.ToListAsync();
                return Json(allCategories);
            }

            var result = await _context.Categories
                .Where(c => c.Name.Contains(keyword))
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    name = c.Name
                })
                .ToListAsync();

            return Json(result);
        }
    }
}
