using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [RegularExpression(@"^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*@[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$",
                          ErrorMessage = "Email không hợp lệ. Vui lòng nhập email hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
