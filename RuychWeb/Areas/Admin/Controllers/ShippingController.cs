using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShippingController : Controller
    {
        private readonly DataContext _dataContext;
        public ShippingController(DataContext context)
        {
            this._dataContext = context;
        }
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            // Lấy tổng số bản ghi trong cơ sở dữ liệu
            var totalShippings = await _dataContext.Shippings.CountAsync();

            // Lấy danh sách shipping theo trang
            var shippings = await _dataContext.Shippings
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalShippings / (double)pageSize);

            // Lưu thông tin phân trang vào ViewBag
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            ViewBag.Shippings = shippings;
            return View();
        }

        [HttpPost]
        [Route("StoreShipping")]
        public async Task<IActionResult> StoreShipping(Shipping shippingModel, string phuong, string quan, string tinh, decimal price)
        {

            shippingModel.City = tinh;
            shippingModel.District = quan;
            shippingModel.Ward = phuong;
            shippingModel.Price = price;

            try
            {

                var existingShipping = await _dataContext.Shippings
                    .AnyAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

                if (existingShipping)
                {
                    return Ok(new { duplicate = true, message = "Dữ liệu trùng lặp." });
                }
                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm shipping thành công" });
            }
            catch (Exception)
            {

                return StatusCode(500, "An error occurred while adding shipping.");
            }
        }
        [HttpPost]
        public IActionResult GetShippingFee(string city, string district, string ward)
        {
            var shipping = _dataContext.Shippings
                .FirstOrDefault(s => s.City == city && s.District == district && s.Ward == ward);

            if (shipping != null)
            {
                return Json(new { success = true, shippingFee = shipping.Price });
            }

            return Json(new { success = false, message = "Không tìm thấy phí vận chuyển cho địa điểm này." });
        }
        public async Task<IActionResult> Delete(int Id)
        {
            Shipping shipping = await _dataContext.Shippings.FindAsync(Id);

            _dataContext.Shippings.Remove(shipping);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Shipping đã được xóa thành công";
            return RedirectToAction("Index");
        }
    }
}
