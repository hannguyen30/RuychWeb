using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShippingController : Controller
    {
        private readonly string _shippingFilePath = "wwwroot/js/shipping.json"; // Đường dẫn tới file shipping.json

        // Phương thức này sẽ đọc file JSON và trả về phí vận chuyển
        [HttpPost]
        public async Task<IActionResult> GetShippingFee(string city, string district, string ward)
        {
            // Đọc dữ liệu từ file shipping.json
            var json = await System.IO.File.ReadAllTextAsync(_shippingFilePath);
            var shippingList = JsonSerializer.Deserialize<List<Shipping>>(json);

            if (shippingList != null)
            {
                // Tìm phí vận chuyển dựa trên city, district, ward
                var shipping = shippingList.FirstOrDefault(s => s.City == city && s.District == district && s.Ward == ward);

                if (shipping != null)
                {
                    return Json(new { success = true, shippingFee = shipping.Price });
                }
                else
                {
                    // Nếu không tìm thấy địa điểm, trả về phí vận chuyển mặc định là 40
                    return Json(new { success = true, shippingFee = 40000 });
                }
            }

            // Nếu không thể đọc file hoặc không tìm thấy dữ liệu, trả về phí vận chuyển mặc định là 40
            return Json(new { success = false, message = "Không tìm thấy dữ liệu phí vận chuyển." });
        }
    }
    public class Shipping
    {
        public string City { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public decimal Price { get; set; }
    }
}
