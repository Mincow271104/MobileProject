using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using be.Models;
using be.Models.DTOs;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
namespace be.Repositories
{


    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUtilitiesRepository _utilitiesRepository;

        public AccountRepository(ApplicationDbContext context, IConfiguration configuration, IUtilitiesRepository utilitiesRepository)
        {
            _context = context;
            _configuration = configuration;
            _utilitiesRepository = utilitiesRepository;
        }

        private async Task<object> ValidateAccountInput(RegisterRequest request)
        {
            if (request == null)
                return new { errCode = -1, errMessage = "Thiếu thông tin người dùng!", data = (object?)null };

            if (string.IsNullOrEmpty(request.AccountName))
                return new { errCode = -1, errMessage = "Tên tài khoản không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.AccountName.Trim(), @"^[a-zA-Z0-9_]{5,50}$"))
                return new { errCode = 1, errMessage = "Tên tài khoản sai định dạng!", data = (object?)null };

            if (string.IsNullOrEmpty(request.Email))
                return new { errCode = -1, errMessage = "Email không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.Email.Trim(), @"^(?=.{5,100}$)[^\s@]+@[^\s@]+\.[^\s@]+$"))
                return new { errCode = 1, errMessage = "Email sai định dạng!", data = (object?)null };

            if (string.IsNullOrEmpty(request.Password))
                return new { errCode = -1, errMessage = "Mật khẩu không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.Password.Trim(), @"^[A-Za-z\d!@#$%^&*]{8,}$"))
                return new { errCode = 1, errMessage = "Mật khẩu không hợp lệ! (Cần ít nhất 8 ký tự)", data = (object?)null };

            if (string.IsNullOrEmpty(request.UserName))
                return new { errCode = -1, errMessage = "Tên người dùng không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.UserName.Trim(), @"^[A-Za-zÀ-ỹ0-9\s]{2,50}$"))
                return new { errCode = 1, errMessage = "Tên người dùng không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.Phone))
                return new { errCode = -1, errMessage = "Số điện thoại không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.Phone.Trim(), @"^[0-9]{10,11}$"))
                return new { errCode = 1, errMessage = "Số điện thoại không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.Address) || request.Address.Trim().Length > 100)
                return new { errCode = -1, errMessage = "Địa chỉ không hợp lệ hoặc vượt quá 100 ký tự!", data = (object?)null };

            if (string.IsNullOrEmpty(request.Gender))
                return new { errCode = -1, errMessage = "Giới tính không được để trống!", data = (object?)null };
            if (!await _utilitiesRepository.CheckValidAllCodeAsync("Gender", request.Gender))
                return new { errCode = 1, errMessage = "Giới tính không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.AccountType))
                return new { errCode = -1, errMessage = "Quyền hạn không được để trống!", data = (object?)null };
            if (!await _utilitiesRepository.CheckValidAllCodeAsync("AccountType", request.AccountType))
                return new { errCode = 1, errMessage = "Quyền hạn không hợp lệ!", data = (object?)null };

            return null;
        }

        private async Task<object> ValidateAccountEdit(ChangeAccountInfoRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.AccountId))
                return new { errCode = -1, errMessage = "Thiếu thông tin chỉnh sửa!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.AccountName) && !Regex.IsMatch(request.AccountName.Trim(), @"^[a-zA-Z0-9_]{5,50}$"))
                return new { errCode = 1, errMessage = "Tên tài khoản sai định dạng!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.UserName) && !Regex.IsMatch(request.UserName.Trim(), @"^[A-Za-zÀ-ỹ0-9\s]{2,50}$"))
                return new { errCode = 1, errMessage = "Tên người dùng không hợp lệ!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.Phone) && !Regex.IsMatch(request.Phone.Trim(), @"^[0-9]{10,11}$"))
                return new { errCode = 1, errMessage = "Số điện thoại không hợp lệ!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.Address) && (request.Address.Trim().Length == 0 || request.Address.Trim().Length > 100))
                return new { errCode = 1, errMessage = "Địa chỉ không hợp lệ hoặc vượt quá 100 ký tự!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.Gender) && !await _utilitiesRepository.CheckValidAllCodeAsync("Gender", request.Gender))
                return new { errCode = 1, errMessage = "Giới tính không hợp lệ!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.AccountType) && !await _utilitiesRepository.CheckValidAllCodeAsync("AccountType", request.AccountType))
                return new { errCode = 1, errMessage = "Quyền hạn không hợp lệ!", data = (object?)null };

            return null;
        }

        private async Task<bool> CheckAccountNameExist(string accountName)
        {
            return await _context.Accounts.AnyAsync(a => a.AccountName == accountName);
        }

        private async Task<bool> CheckEmailExist(string email)
        {
            return await _context.Accounts.AnyAsync(a => a.Email == email);
        }

        private async Task<bool> CheckPhoneExist(string phone)
        {
            return await _context.Accounts.AnyAsync(a => a.Phone == phone);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new Exception("Mật khẩu không hợp lệ!");
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private string FirstNavigate(string accountType)
        {
            if (string.IsNullOrEmpty(accountType))
                return "/login";

            switch (accountType)
            {
                case "A":
                    return "/user/admin";
                case "O":
                    return "/user/owner";
                case "C":
                    return "/home";
                default:
                    return "/login";
            }
        }

        private string CreateJWT(object data, bool rememberLogin)
        {
            if (data == null)
                return null;

            var claims = new List<Claim>();
            var dataDict = data.GetType()
                .GetProperties()
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(data)?.ToString());

            foreach (var kvp in dataDict)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    claims.Add(new Claim(kvp.Key, kvp.Value));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresIn = rememberLogin
                ? TimeSpan.FromDays(7)
                : TimeSpan.FromHours(1);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.Now.Add(expiresIn),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private object VerifyJWT(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out SecurityToken validatedToken);

                return (validatedToken as JwtSecurityToken)?.Claims
                    .ToDictionary(c => c.Type, c => c.Value);
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> SendVerificationEmailAsync(string email, string code)
        {
            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential(_configuration["Email:User"], _configuration["Email:Pass"]),
                    EnableSsl = true,
                };

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_configuration["Email:User"]),
                    Subject = "Mã xác nhận đặt lại mật khẩu",
                    Body = $"Mã xác nhận của bạn là: {code}. Mã này có hiệu lực trong 30 phút.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<object> UserRegisterAsync(RegisterRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var validationResult = await ValidateAccountInput(request);
                if (validationResult != null)
                    return validationResult;

                if (await CheckAccountNameExist(request.AccountName))
                    return new { errCode = 1, errMessage = "Tên tài khoản đã tồn tại trong hệ thống!", data = (object?)null };

                if (await CheckEmailExist(request.Email))
                    return new { errCode = 1, errMessage = "Email đã tồn tại trong hệ thống!", data = (object?)null };

                if (await CheckPhoneExist(request.Phone))
                    return new { errCode = 1, errMessage = "Số điện thoại đã tồn tại trong hệ thống!", data = (object?)null };

                var accountId = await _utilitiesRepository.GenerateIDAsync(request.AccountType ?? "C", 9, "Accounts", "AccountID");
                var hashedPassword = HashPassword(request.Password);

                var account = new Account
                {
                    AccountID = accountId,
                    AccountName = request.AccountName.Trim(),
                    Email = request.Email.Trim(),
                    Password = hashedPassword,
                    UserName = request.UserName.Trim(),
                    UserImage = "https://res.cloudinary.com/dqblg6ont/image/upload/v1744579137/tgx7fjbmpulisg3emlts.jpg",
                    Phone = request.Phone.Trim(),
                    Address = request.Address.Trim(),
                    Gender = request.Gender,
                    LoginAttempt = 0,
                    LockUntil = null,
                    CreatedAt = DateTime.Now,
                    AccountStatus = "ACT",
                    AccountType = request.AccountType ?? "C"
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new { errCode = 0, errMessage = "Đăng ký người dùng thành công!", data = new { AccountID = accountId } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi đăng ký: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> UserLoginAsync(LoginRequest request, HttpResponse response)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null)
                    return new { errCode = -1, errMessage = "Thiếu thông tin đăng nhập!", data = (object?)null };

                if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
                    return new { errCode = -1, errMessage = "Tên tài khoản hoặc mật khẩu không được để trống!", data = (object?)null };

                var existedAccount = await _context.Accounts
                    .Where(a => a.AccountName == request.AccountName)
                    .Select(a => new
                    {
                        a.AccountID,
                        a.AccountName,
                        a.Password,
                        a.UserName,
                        a.UserImage,
                        a.LoginAttempt,
                        a.LockUntil,
                        a.AccountStatus,
                        a.AccountType
                    })
                    .FirstOrDefaultAsync();

                if (existedAccount == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };
                }

                var currentTime = DateTime.Now;
                if (existedAccount.LockUntil.HasValue && existedAccount.LockUntil > currentTime)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Tài khoản đang bị khóa. Vui lòng thử lại sau!", data = (object?)null };
                }

                var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, existedAccount.Password);
                if (isPasswordValid && existedAccount.AccountStatus == "ACT")
                {
                    var accountData = new
                    {
                        existedAccount.AccountID,
                        existedAccount.AccountName,
                        existedAccount.UserName,
                        existedAccount.UserImage,
                        existedAccount.AccountStatus,
                        existedAccount.AccountType,
                        Navigate = FirstNavigate(existedAccount.AccountType)
                    };

                    await _context.Accounts
                        .Where(a => a.AccountName == request.AccountName)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(a => a.LoginAttempt, 0)
                            .SetProperty(a => a.LockUntil, (DateTime?)null));

                    var jwtToken = CreateJWT(accountData, request.RememberLogin);
                    if (!string.IsNullOrEmpty(jwtToken))
                    {
                        response.Cookies.Append("token", jwtToken, new CookieOptions
                        {
                            HttpOnly = true,
                            MaxAge = request.RememberLogin ? TimeSpan.FromDays(7) : TimeSpan.FromHours(1)
                        });
                    }

                    await transaction.CommitAsync();
                    return new { errCode = 0, errMessage = "Đăng nhập thành công!", data = accountData };
                }
                else
                {
                    var newLoginAttempts = (existedAccount.LoginAttempt ?? 0) + 1;
                    DateTime? lockUntilTime = null;

                    if (newLoginAttempts >= 5)
                    {
                        lockUntilTime = currentTime.AddMinutes(5);
                        newLoginAttempts = 0;
                    }

                    await _context.Accounts
                        .Where(a => a.AccountName == request.AccountName)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(a => a.LoginAttempt, newLoginAttempts)
                            .SetProperty(a => a.LockUntil, lockUntilTime));

                    await transaction.CommitAsync();
                    return new
                    {
                        errCode = 2,
                        errMessage = newLoginAttempts == 0
                            ? "Đã vượt quá số lần đăng nhập. Tài khoản bị khóa 5 phút!"
                            : "Sai mật khẩu!",
                        data = (object?)null
                    };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi đăng nhập: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> UserLogoutAsync(string token, HttpRequest request, HttpResponse response)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    response.Cookies.Delete("token");
                    await transaction.RollbackAsync();
                    return new { errCode = -1, errMessage = "Không có token để đăng xuất!", data = (object?)null };
                }

                var decoded = VerifyJWT(token) as Dictionary<string, string>;
                await _context.BlacklistTokens
                    .Where(bt => bt.ExpiredAt < DateTime.Now)
                    .ExecuteDeleteAsync();

                var expiredAt = decoded != null && decoded.ContainsKey("exp")
                    ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(decoded["exp"])).DateTime
                    : DateTime.Now.AddDays(1);

                _context.BlacklistTokens.Add(new BlacklistToken
                {
                    Token = token,
                    CreatedAt = DateTime.Now,
                    ExpiredAt = expiredAt
                });

                await _context.SaveChangesAsync();
                response.Cookies.Delete("token");
                await transaction.CommitAsync();

                return new { errCode = 0, errMessage = "Đăng xuất thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                response.Cookies.Delete("token");
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi đăng xuất: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> VerifyTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return new { errCode = -1, errMessage = "Không có token!", data = (object?)null };

                var isBlacklisted = await _context.BlacklistTokens
                    .AnyAsync(bt => bt.Token == token);

                if (isBlacklisted)
                    return new { errCode = 2, errMessage = "Token đã bị vô hiệu hóa!", data = (object?)null };

                var data = VerifyJWT(token);
                if (data != null)
                    return new { errCode = 0, errMessage = "Token hợp lệ!", data };

                return new { errCode = 2, errMessage = "Token không hợp lệ hoặc đã hết hạn!", data = (object?)null };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi xác minh token: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetAccountInfoAsync(string accountId)
        {
            try
            {
                if (string.IsNullOrEmpty(accountId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                if (accountId == "ALL")
                {
                    var userData = await _context.Accounts
                        .Select(a => new
                        {
                            a.AccountID,
                            a.AccountName,
                            a.Email,
                            a.UserName,
                            a.UserImage,
                            a.Phone,
                            a.Address,
                            a.Gender,
                            a.CreatedAt,
                            a.AccountStatus,
                            a.AccountType
                        })
                        .ToListAsync();

                    if (!userData.Any())
                        return new { errCode = 1, errMessage = "Không tìm thấy tài khoản nào!", data = new List<object>() };

                    return new { errCode = 0, errMessage = "Lấy thông tin tài khoản thành công!", data = userData };
                }
                else
                {
                    var userData = await _context.Accounts
                        .Where(a => a.AccountID == accountId)
                        .Select(a => new
                        {
                            a.AccountID,
                            a.AccountName,
                            a.Email,
                            a.UserName,
                            a.UserImage,
                            a.Phone,
                            a.Address,
                            a.Gender,
                            a.CreatedAt,
                            a.AccountStatus,
                            a.AccountType
                        })
                        .FirstOrDefaultAsync();

                    if (userData == null)
                        return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };

                    return new { errCode = 0, errMessage = "Lấy thông tin tài khoản thành công!", data = userData };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadAccountInfoAsync(int page, int limit, string search, string filter, string sort)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                var query = _context.Accounts.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 100));
                    query = query.Where(a => a.Email.Contains(searchTerm) || a.UserName.Contains(searchTerm) || a.Phone.Contains(searchTerm));
                }

                if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];
                    var fieldMap = new Dictionary<string, string>
                    {
                        { "accounttype", "AccountType" },
                        { "gender", "Gender" },
                        { "accountstatus", "AccountStatus" }
                    };

                    if (!fieldMap.ContainsKey(field))
                        return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                    if (!await _utilitiesRepository.CheckValidAllCodeAsync(fieldMap[field], value))
                        return new { errCode = 1, errMessage = $"{fieldMap[field]} không hợp lệ!", data = (object?)null };

                    query = query.Where(a => EF.Property<string>(a, fieldMap[field]) == value);
                }

                query = sort switch
                {
                    "1" => query.OrderBy(a => a.UserName),
                    "2" => query.OrderByDescending(a => a.UserName),
                    "4" => query.OrderByDescending(a => a.CreatedAt),
                    "5" => query.OrderBy(a => a.CreatedAt),
                    _ => query.OrderByDescending(a => a.AccountID)
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.AccountID,
                        a.AccountName,
                        a.Email,
                        a.UserName,
                        a.UserImage,
                        a.Phone,
                        a.Address,
                        a.Gender,
                        a.CreatedAt,
                        a.AccountStatus,
                        a.AccountType
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy tài khoản nào!", data = new List<object>(), totalItems = 0 };

                return new { errCode = 0, errMessage = "Lấy danh sách tài khoản thành công!", data = rows, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeAccountInfoAsync(ChangeAccountInfoRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.AccountId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var validationResult = await ValidateAccountEdit(request);
                if (validationResult != null)
                    return validationResult;

                var account = await _context.Accounts
                    .Where(a => a.AccountID == request.AccountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };
                }

                if (!string.IsNullOrEmpty(request.AccountName) && request.AccountName != account.AccountName)
                {
                    if (await CheckAccountNameExist(request.AccountName))
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = "Tên tài khoản đã tồn tại trong hệ thống!", data = (object?)null };
                    }
                }

                if (!string.IsNullOrEmpty(request.Phone) && request.Phone != account.Phone)
                {
                    if (await CheckPhoneExist(request.Phone))
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = "Số điện thoại đã tồn tại trong hệ thống!", data = (object?)null };
                    }
                }

                bool isUpdated = false;
                if (!string.IsNullOrEmpty(request.AccountName))
                {
                    account.AccountName = request.AccountName;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.UserName))
                {
                    account.UserName = request.UserName;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.Gender))
                {
                    account.Gender = request.Gender;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.Phone))
                {
                    account.Phone = request.Phone;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.Address))
                {
                    account.Address = request.Address;
                    isUpdated = true;
                }
                if (request.UserImage != null)
                {
                    if (request.UserImage == null)
                    {
                        account.UserImage = null;
                        isUpdated = true;
                    }
                    else if (string.IsNullOrEmpty(request.UserImage) || request.UserImage.Length > 2048)
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = "URL ảnh đại diện không hợp lệ hoặc vượt quá 2048 ký tự!", data = (object?)null };
                    }
                    else
                    {
                        account.UserImage = request.UserImage;
                        isUpdated = true;
                    }
                }
                if (!string.IsNullOrEmpty(request.AccountType))
                {
                    account.AccountType = request.AccountType;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Không có thông tin nào để cập nhật!", data = (object?)null };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật thông tin tài khoản thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> SendForgotTokenAsync(string email)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };
                }

                if (string.IsNullOrEmpty(_configuration["Email:User"]) || string.IsNullOrEmpty(_configuration["Email:Pass"]))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 3, errMessage = "Cấu hình email không hợp lệ!", data = (object?)null };
                }

                var emailTrimmed = email.Trim();
                if (!Regex.IsMatch(emailTrimmed, @"^(?=.{5,100}$)[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Email không hợp lệ!", data = (object?)null };
                }

                var account = await _context.Accounts
                    .Where(a => a.Email == emailTrimmed)
                    .Select(a => new { a.AccountID })
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Email không tồn tại trong hệ thống!", data = (object?)null };
                }

                var recentTokens = await _context.BlacklistTokens
                    .CountAsync(bt => bt.ExtraValue == account.AccountID && bt.CreatedAt > DateTime.Now.AddMinutes(-1));

                if (recentTokens >= 5)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Vượt quá số lần gửi mã xác nhận!", data = (object?)null };
                }

                var code = new Random().Next(100000, 999999).ToString();
                var currentTime = DateTime.Now;
                var expiredAt = currentTime.AddMinutes(30);

                await _context.BlacklistTokens
                    .Where(bt => bt.ExtraValue == account.AccountID && bt.ExpiredAt < currentTime)
                    .ExecuteDeleteAsync();

                _context.BlacklistTokens.Add(new BlacklistToken
                {
                    Token = code,
                    ExtraValue = account.AccountID,
                    CreatedAt = currentTime,
                    ExpiredAt = expiredAt
                });

                var emailSent = await SendVerificationEmailAsync(emailTrimmed, code);
                if (!emailSent)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 3, errMessage = "Lỗi khi gửi email xác nhận!", data = (object?)null };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Mã xác nhận đã được gửi!", data = account.AccountID };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi xử lý email: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> VerifyForgotTokenAsync(string accountId, string token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(token))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };
                }

                var currentTime = DateTime.Now;
                var tokenRecord = await _context.BlacklistTokens
                    .Where(bt => bt.Token == token && bt.ExtraValue == accountId)
                    .FirstOrDefaultAsync();

                if (tokenRecord == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Mã xác nhận không hợp lệ!", data = (object?)null };
                }

                if (tokenRecord.ExpiredAt < currentTime)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Mã xác nhận đã hết hạn!", data = (object?)null };
                }

                await _context.BlacklistTokens
                    .Where(bt => bt.ExtraValue == accountId)
                    .ExecuteDeleteAsync();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Xác nhận mã thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi xác minh mã: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangePasswordAsync(string accountId, string password, string newPassword)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(newPassword))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };
                }

                var account = await _context.Accounts
                    .Where(a => a.AccountID == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };
                }

                if (password != "forgot_password")
                {
                    var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, account.Password);
                    if (!isPasswordValid)
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 2, errMessage = "Mật khẩu cũ không đúng!", data = (object?)null };
                    }
                }

                var newPasswordTrimmed = newPassword.Trim();
                if (!Regex.IsMatch(newPasswordTrimmed, @"^[A-Za-z\d!@#$%^&*]{8,}$"))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Mật khẩu mới không hợp lệ! (Cần ít nhất 8 ký tự)", data = (object?)null };
                }

                if (password != "forgot_password" && BCrypt.Net.BCrypt.Verify(newPassword, account.Password))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Mật khẩu mới không được trùng với mật khẩu cũ!", data = (object?)null };
                }

                var hashedPassword = HashPassword(newPasswordTrimmed);
                account.Password = hashedPassword;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Đổi mật khẩu thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi đổi mật khẩu: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeAccountStatusAsync(string accountId, string accountStatus)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(accountStatus))
                {
                    await transaction.RollbackAsync();
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };
                }

                var validAccountStatus = await _utilitiesRepository.CheckValidAllCodeAsync("AccountStatus", accountStatus);
                if (!validAccountStatus)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Trạng thái tài khoản không hợp lệ!", data = (object?)null };
                }

                var account = await _context.Accounts
                    .Where(a => a.AccountID == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };
                }

                if (account.AccountStatus == accountStatus)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Trạng thái không thay đổi!", data = (object?)null };
                }

                account.AccountStatus = accountStatus;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Thay đổi trạng thái tài khoản thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi thay đổi trạng thái: {ex.Message}", data = (object?)null };
            }
        }
    }
}