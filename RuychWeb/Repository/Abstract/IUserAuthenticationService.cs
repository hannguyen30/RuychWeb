﻿using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;

namespace RuychWeb.Repository.Abstract
{
    public interface IUserAuthenticationService
    {
        Task<Status> RegisterAsync(RegistrationModel model, IHttpContextAccessor httpContextAccessor);
        Task<Status> ConfirmEmailAsync(string userId, string token);
        Task<Status> LoginAsync(LoginModel model);
        Task LogoutAsync();
        Task<Status> ChangePasswordAsync(ChangePasswordModel model, string email);
        Task<Status> SendPasswordResetLinkAsync(string email, IHttpContextAccessor httpContextAccessor);
        Task<Status> ResetPasswordAsync(ResetPasswordModel model);
        Task<Account> GetUserByEmailAsync(string email);
    }
}