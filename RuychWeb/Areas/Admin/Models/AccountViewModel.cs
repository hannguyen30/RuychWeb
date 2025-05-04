using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Areas.Admin.Models
{
    public class AccountViewModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        [RegularExpression(@"^(0|\+84)(\d{9})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Address { get; set; }  // Dành cho Customer
        public DateTime? Birthday { get; set; }
    }
}