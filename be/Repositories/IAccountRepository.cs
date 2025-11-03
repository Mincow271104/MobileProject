using be.Models.DTOs;
using System.Threading.Tasks;

namespace be.Repositories
{
    public interface IAccountRepository
    {
        Task<object> UserRegisterAsync(RegisterRequest request);
        Task<object> UserLoginAsync(LoginRequest request, HttpResponse response);
        Task<object> UserLogoutAsync(string token, HttpRequest request, HttpResponse response);
        Task<object> VerifyTokenAsync(string token);
        Task<object> GetAccountInfoAsync(string accountId);
        Task<object> LoadAccountInfoAsync(int page, int limit, string search, string filter, string sort);
        Task<object> ChangeAccountInfoAsync(ChangeAccountInfoRequest request);
        Task<object> SendForgotTokenAsync(string email);
        Task<object> VerifyForgotTokenAsync(string accountId, string token);
        Task<object> ChangePasswordAsync(string accountId, string password, string newPassword);
        Task<object> ChangeAccountStatusAsync(string accountId, string accountStatus);
    }
}