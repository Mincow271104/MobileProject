namespace be.Controllers
{
    using be.Models.DTOs;
    using be.Repositories;
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;

        public AccountApiController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _accountRepository.UserRegisterAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _accountRepository.UserLoginAsync(request, Response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = Request.Cookies["token"];
                var response = await _accountRepository.UserLogoutAsync(token, Request, Response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Response.Cookies.Delete("token");
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("verify-token")]
        public async Task<IActionResult> VerifyToken()
        {
            try
            {
                var token = Request.Cookies["token"];
                var response = await _accountRepository.VerifyTokenAsync(token);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-account-info")]
        public async Task<IActionResult> GetAccountInfo([FromQuery] string accountid)
        {
            try
            {
                var response = await _accountRepository.GetAccountInfoAsync(accountid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-account-info")]
        public async Task<IActionResult> LoadAccountInfo([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0")
        {
            try
            {
                var response = await _accountRepository.LoadAccountInfoAsync(page, limit, search, filter, sort);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-account-info")]
        public async Task<IActionResult> ChangeAccountInfo([FromBody] ChangeAccountInfoRequest request)
        {
            try
            {
                var response = await _accountRepository.ChangeAccountInfoAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("send-forgot-token")]
        public async Task<IActionResult> SendForgotToken([FromBody] SendForgotTokenRequest request)
        {
            try
            {
                var response = await _accountRepository.SendForgotTokenAsync(request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("verify-forgot-token")]
        public async Task<IActionResult> VerifyForgotToken([FromBody] VerifyForgotTokenRequest request)
        {
            try
            {
                var response = await _accountRepository.VerifyForgotTokenAsync(request.AccountId, request.Token);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var response = await _accountRepository.ChangePasswordAsync(request.AccountId, request.Password, request.NewPassword);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-account-status")]
        public async Task<IActionResult> ChangeAccountStatus([FromBody] ChangeAccountStatusRequest request)
        {
            try
            {
                var response = await _accountRepository.ChangeAccountStatusAsync(request.AccountId, request.AccountStatus);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}