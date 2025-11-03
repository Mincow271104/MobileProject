using be.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

// Ghi chú: Repository cho AllCodes, ánh xạ logic từ getAllCodes trong utilitiesService.js
namespace be.Repositories
{
    public class UtilitiesRepository : IUtilitiesRepository
    {
        private readonly ApplicationDbContext _context;

        public UtilitiesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAllCodesAsync(string type)
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                if (type == "ALL")
                {
                    var types = await _context.AllCodes
                        .Select(c => new { c.Type })
                        .Distinct()
                        .ToListAsync();

                    return new { errCode = 0, errMessage = "Lấy danh sách mã thành công!", data = types.Any() ? (object)types : new List<object>() };
                }
                else
                {
                    var codes = await _context.AllCodes
                        .Where(c => c.Type == type)
                        .Select(c => new { Code = c.Code, CodeValueVI = c.CodeValueVI, ExtraValue = c.ExtraValue })
                        .ToListAsync();

                    return new { errCode = 0, errMessage = "Lấy danh sách mã thành công!", data = codes.Any() ? (object)codes : new List<object>() };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách mã: {ex.Message}", data = (object?)null };
            }
        }
        public async Task<bool> CheckValidAllCodeAsync(string type, string code)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(code))
                return false;

            return await _context.AllCodes.AnyAsync(c => c.Type == type && c.Code == code);
        }

        public async Task<string> GenerateIDAsync(string prefix, int digitCount, string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(prefix) || digitCount < 1 || digitCount > 15 || string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                throw new Exception("Thiếu tham số để tạo ID!");

            if (!Regex.IsMatch(prefix, @"^[A-Za-z0-9]{1,10}$"))
                throw new Exception("Prefix không hợp lệ (1-10 ký tự, chỉ chữ và số)!");

            string id;
            bool exists;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                var timestampDigits = timestamp.Substring(timestamp.Length - Math.Min(digitCount, timestamp.Length)).PadLeft(digitCount, '0');
                id = $"{prefix}{timestampDigits}";
                exists = await _context.Accounts.AnyAsync(a => EF.Property<string>(a, columnName) == id);
                attempts++;
            } while (exists && attempts < maxAttempts);

            if (attempts >= maxAttempts)
                throw new Exception("Tạo mã thất bại sau nhiều lần thử!");

            return id;
        }
    }
}