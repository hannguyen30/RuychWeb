using RuychWeb.Models.Vnpay;

namespace RuychWeb.Repository.Services.Vnpay
{
    public interface IVnPayService
    {

        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
