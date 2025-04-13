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
        public async Task<IActionResult> Index()
        {
            var shippingList = await _dataContext.Shippings.ToListAsync();
            ViewBag.Shippings = shippingList;
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
