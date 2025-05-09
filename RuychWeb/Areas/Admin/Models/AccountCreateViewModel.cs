using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Areas.Admin.Models
{
    public class AccountCreateViewModel
    {
        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [RegularExpression(@"^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*@[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$",
                          ErrorMessage = "Email không hợp lệ. Vui lòng nhập email hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{6,}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường và 1 ký tự đặc biệt, tối thiểu 6 ký tự")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Mật khẩu không trùng khớp")]
        public string ConfirmPassword { get; set; }

    }
}
