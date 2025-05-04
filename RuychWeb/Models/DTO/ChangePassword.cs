using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được bỏ trống")]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*[#$^+=!*()@%&]).{6,}$", ErrorMessage = "Mật khẩu phải có 6 ký tự bao gồm 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt.")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string? PasswordConfirm { get; set; }

    }
}
