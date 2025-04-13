
namespace RuychWeb.Models.ViewModels
{
    public class OrderViewModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Tinh { get; set; }
        public string Quan { get; set; }
        public string Phuong { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalFee { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}