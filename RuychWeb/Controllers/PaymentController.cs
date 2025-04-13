using Microsoft.AspNetCore.Mvc;
using RuychWeb.Models.Vnpay;
using RuychWeb.Repository.Services.Vnpay;

namespace RuychWeb.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        public PaymentController(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        [HttpPost]
        public IActionResult CreateVnPayUrl([FromForm] PaymentInformationModel model)
        {
            if (model == null || model.Amount <= 0)
            {
                return Json(new { success = false, message = "Invalid payment information." });
            }

            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);
                return Json(new { success = true, paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
