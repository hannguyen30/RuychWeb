
using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.ViewModels
{
    public class OrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải chứa 10 chữ số.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string Address { get; set; }
        public string Tinh { get; set; }
        public string Quan { get; set; }
        public string Phuong { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalFee { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
        public int? OrderId { get; set; }
    }
}