using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuychWeb.Models.DTO;
using RuychWeb.Repository.Abstract;

namespace RuychWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserAuthenticationService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(IUserAuthenticationService authService, IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Registration(RegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                var status = await _authService.RegisterAsync(model, _httpContextAccessor);
                if (status.StatusCode == 1)
                {
                    TempData["msg"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                    return RedirectToAction("AccountNotVerified");
                }
                if (status.Message.Contains("Email này đã được sử dụng"))
                {
                    ModelState.AddModelError("Email", status.Message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, status.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult AccountNotVerified()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var status = await _authService.LoginAsync(model);
                if (status.StatusCode == 1)
                {
                    var user = await _authService.GetUserByEmailAsync(model.Email); // Assuming you have a method to get user by email
                    ViewBag.UserName = user.UserName;
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, status.Message);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var email = User.Identity.Name; // Get the logged-in user's email
                var status = await _authService.ChangePasswordAsync(model, email);
                if (status.StatusCode == 1)
                {
                    TempData["msg"] = "Password changed successfully!";
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, status.Message);
            }
            TempData["PasswordChanged"] = "Mật khẩu đã được thay đổi thành công!";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await this._authService.LogoutAsync();
            return RedirectToAction(nameof(Login));
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var status = await _authService.SendPasswordResetLinkAsync(model.Email, _httpContextAccessor);
                if (status.StatusCode == 1)
                {
                    TempData["msg"] = "Password reset sent! Please check your email.";
                    return RedirectToAction("Login");
                }
                ModelState.AddModelError(string.Empty, status.Message);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            return View(new ResetPasswordModel { Email = email, Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var status = await _authService.ResetPasswordAsync(model);
                if (status.StatusCode == 1)
                {
                    TempData["msg"] = "Password reset successfully!";
                    return RedirectToAction("Login");
                }
                ModelState.AddModelError(string.Empty, status.Message);
            }
            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var status = await _authService.ConfirmEmailAsync(userId, token);
            if (status.StatusCode == 1)
            {
                TempData["msg"] = "Email confirmed successfully!";
                return RedirectToAction("Login");
            }
            TempData["msg"] = "Email confirmation failed!";
            return RedirectToAction("Login");
        }
    }
}

