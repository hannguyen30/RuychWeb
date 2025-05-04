using Microsoft.AspNetCore.Identity;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using RuychWeb.Repository.Abstract;

namespace RuychWeb.Repository.Implementation
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<Account> userManager;
        private readonly SignInManager<Account> signInManager;
        private readonly EmailService emailService;
        private readonly LinkGenerator linkGenerator;
        private static Dictionary<string, int> loginAttempts = new Dictionary<string, int>();
        private static Dictionary<string, DateTime> lockedUsers = new Dictionary<string, DateTime>();
        private readonly DataContext _dataContext;
        public UserAuthenticationService(UserManager<Account> userManager,
            SignInManager<Account> signInManager, EmailService emailService, LinkGenerator linkGenerator, DataContext dataContext)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailService = emailService;
            this.linkGenerator = linkGenerator;
            this._dataContext = dataContext;
        }

        public async Task<Status> RegisterAsync(RegistrationModel model, IHttpContextAccessor httpContextAccessor)
        {
            var status = new Status();
            var userExists = await userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                status.StatusCode = 0;
                status.Message = "Email này đã được sử dụng, vui lòng chọn email khác";
                return status;
            }
            Account user = new Account()
            {
                Email = model.Email,
                UserName = model.Email.Split('@')[0],
                EmailConfirmed = false,
            };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Tạo tài khoản không thành công";
                return status;
            }
            var roleResult = await userManager.AddToRoleAsync(user, "Customer");
            if (!roleResult.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Cấp quyền thất bại";
                return status;
            }

            var customer = new Customer()
            {
                AccountId = user.Id,  // Liên kết Customer với Account
                Name = null,           // Có thể để null hoặc giá trị mặc định
                Phone = null,          // Trống hoặc giá trị mặc định
                Email = user.Email,    // Gán email của người dùng vào customer
                Address = ""     // Trống hoặc giá trị mặc định
            };
            await _dataContext.Customers.AddAsync(customer);
            await _dataContext.SaveChangesAsync();

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var request = httpContextAccessor.HttpContext.Request;
            var confirmationLink = $"{request.Scheme}://{request.Host}/Account/ConfirmEmail?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            await emailService.SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");

            status.StatusCode = 1;
            status.Message = "Tài khoản đã được tạo, vui lòng xác thực.";
            return status;
        }

        public async Task<Status> ConfirmEmailAsync(string userId, string token)
        {
            var status = new Status();
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                status.StatusCode = 0;
                status.Message = "Tài khoản không tồn tại";
                return status;
            }

            var result = await userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(token));
            if (result.Succeeded)
            {
                status.StatusCode = 1;
                status.Message = "Xác thực thành công";
            }
            else
            {
                status.StatusCode = 0;
                status.Message = "Lỗi xác thực tài khoản";
                foreach (var error in result.Errors)
                {
                    status.Message += $" {error.Description}";
                }
            }

            return status;
        }

        public async Task<Status> LoginAsync(LoginModel model)
        {
            var status = new Status();
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                status.StatusCode = 0;
                status.Message = "Email không tồn tại";
                return status;
            }
            var result = await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Mật khẩu sai";
                return status;
            }
            await userManager.UpdateAsync(user);
            status.StatusCode = 1;
            status.Message = "Đăng nhập thành công";
            return status;
        }

        public async Task<Status> ChangePasswordAsync(ChangePasswordModel model, string email)
        {
            var status = new Status();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                status.StatusCode = 0;
                status.Message = "Tài khoản không tồn tại";
                return status;
            }

            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                status.StatusCode = 1;
                status.Message = "Đổi mật khẩu thành công";
            }
            else
            {
                status.StatusCode = 0;
                status.Message = "Đổi mật khẩu thất bại";
                foreach (var error in result.Errors)
                {
                    status.Message += $" {error.Description}";
                }
            }

            return status;
        }

        public async Task LogoutAsync()
        {
            await signInManager.SignOutAsync();
        }

        public async Task<Status> SendPasswordResetLinkAsync(string email, IHttpContextAccessor httpContextAccessor)
        {
            var status = new Status();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                status.StatusCode = 0;
                status.Message = "Email không tồn tại";
                return status;
            }

            // Generate a new password
            var newPassword = GenerateRandomPassword();

            // Reset the user's password
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!resetResult.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Lỗi không thể cấp mật khẩu";
                foreach (var error in resetResult.Errors)
                {
                    status.Message += $" {error.Description}";
                }
                return status;
            }

            // Send an email with the new password
            var request = httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                status.StatusCode = 0;
                status.Message = "Lỗi không gửi được mail";
                return status;
            }

            await emailService.SendEmailAsync(user.Email ?? string.Empty, "Password Reset", $"Your new password is: {newPassword}");

            status.StatusCode = 1;
            status.Message = "Mật khẩu mới đã được gửi vào email, vui lòng kiểm tra.";
            return status;
        }

        private string GenerateRandomPassword()
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<Status> ResetPasswordAsync(ResetPasswordModel model)
        {
            var status = new Status();
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                status.StatusCode = 0;
                status.Message = "Email không tồn tại";
                return status;
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                status.StatusCode = 1;
                status.Message = "Đổi mật khẩu thành công";
            }
            else
            {
                status.StatusCode = 0;
                status.Message = "Lỗi không thể đổi mật khẩu";
                foreach (var error in result.Errors)
                {
                    status.Message += $" {error.Description}";
                }
            }

            return status;
        }
        public async Task<Account> GetUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }
    }
}