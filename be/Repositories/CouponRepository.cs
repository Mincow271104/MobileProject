using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using be.Models;
using be.Models.DTOs;
namespace be.Repositories
{


    public class CouponRepository : ICouponRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUtilitiesRepository _utilitiesRepository;

        public CouponRepository(ApplicationDbContext context, IUtilitiesRepository utilitiesRepository)
        {
            _context = context;
            _utilitiesRepository = utilitiesRepository;
        }

        private async Task<object> ValidateCouponInput(CouponRequest request)
        {
            if (request == null || request.GetType().GetProperties().All(p => p.GetValue(request) == null))
                return new { errCode = -1, errMessage = "Thiếu thông tin mã giảm giá!", data = (object?)null };

            if (string.IsNullOrEmpty(request.CouponCode))
                return new { errCode = -1, errMessage = "Vui lòng nhập mã giảm giá!", data = (object?)null };

            if (!Regex.IsMatch(request.CouponCode.Trim(), @"^[a-zA-Z0-9]{5,20}$"))
                return new { errCode = 1, errMessage = "Mã giảm giá không hợp lệ hoặc vượt quá giới hạn ký tự!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.CouponDescription) && (request.CouponDescription.Trim().Length == 0 || request.CouponDescription.Trim().Length > 65535))
                return new { errCode = 1, errMessage = "Mô tả giảm giá không hợp lệ hoặc vượt quá giới hạn ký tự!", data = (object?)null };

            if (request.MinOrderValue.HasValue && request.MinOrderValue < 0)
                return new { errCode = -1, errMessage = "Giá trị mua ít nhất không được bé hơn 0!", data = (object?)null };

            if (string.IsNullOrEmpty(request.DiscountType))
                return new { errCode = -1, errMessage = "Loại giảm giá không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("DiscountType", request.DiscountType))
                return new { errCode = 1, errMessage = "Loại giảm giá không hợp lệ!", data = (object?)null };

            if (request.DiscountValue <= 0)
                return new { errCode = -1, errMessage = "Giá trị giảm không được để trống hoặc bé hơn 0!", data = (object?)null };

            if (request.DiscountType == "PERC" && (request.DiscountValue > 100 || request.DiscountValue < 0))
                return new { errCode = 1, errMessage = "Giá trị giảm không hợp lệ!", data = (object?)null };

            if (request.DiscountType == "FIXED" && request.DiscountValue < 0)
                return new { errCode = 1, errMessage = "Giá trị giảm không hợp lệ!", data = (object?)null };

            if (request.MaxDiscount.HasValue && request.MaxDiscount < 0)
                return new { errCode = 1, errMessage = "Giảm giá tối đa phải lớn hơn 0!", data = (object?)null };

            if (request.DiscountType == "FIXED" && request.MaxDiscount.HasValue && request.MaxDiscount > request.DiscountValue)
                return new { errCode = -1, errMessage = "Giảm giá tối đa không được lớn hơn giá trị giảm ban đầu!", data = (object?)null };

            if (request.StartDate == default)
                return new { errCode = -1, errMessage = "Ngày bắt đầu không được để trống!", data = (object?)null };

            if (request.EndDate.HasValue)
            {
                var now = DateTime.Now;
                var endDate = request.EndDate.Value;
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, now.Hour, now.Minute, 0);
                if (endDate < now)
                    return new { errCode = 1, errMessage = "Ngày hết hiệu lực phải trong tương lai!", data = (object?)null };

                if (endDate < request.StartDate)
                    return new { errCode = 1, errMessage = "Thời gian hết hiệu lực phải lớn hơn thời gian bắt đầu!", data = (object?)null };
            }

            if (string.IsNullOrEmpty(request.CouponStatus))
                return new { errCode = -1, errMessage = "Trạng thái giảm giá không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("CouponStatus", request.CouponStatus))
                return new { errCode = 1, errMessage = "Trạng thái giảm giá không hợp lệ!", data = (object?)null };

            return null;
        }

        private async Task<bool> CheckCouponCodeExistAsync(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
                return false;

            return await _context.Coupons.AnyAsync(c => c.CouponCode == couponCode);
        }

        public async Task<object> GetCouponInfoAsync(string couponCode)
        {
            try
            {
                if (string.IsNullOrEmpty(couponCode))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                if (couponCode == "ALL")
                {
                    var coupons = await _context.Coupons
                        .Select(c => new
                        {
                            c.CouponCode,
                            c.MinOrderValue,
                            c.DiscountValue,
                            c.MaxDiscount,
                            c.StartDate,
                            c.EndDate,
                            c.DiscountType,
                            c.CouponStatus
                        })
                        .ToListAsync();

                    return new { errCode = 0, errMessage = "Lấy dữ liệu thành công!", data = coupons };
                }
                else
                {
                    var coupon = await _context.Coupons
                        .Where(c => c.CouponCode == couponCode)
                        .Select(c => new
                        {
                            c.CouponCode,
                            c.MinOrderValue,
                            c.DiscountValue,
                            c.MaxDiscount,
                            c.StartDate,
                            c.EndDate,
                            c.DiscountType,
                            c.CouponStatus
                        })
                        .FirstOrDefaultAsync();

                    if (coupon == null)
                        return new { errCode = 2, errMessage = "Mã giảm giá không tồn tại!", data = (object?)null };

                    return new { errCode = 0, errMessage = "Lấy dữ liệu thành công!", data = coupon };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi kiểm tra thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadCouponInfoAsync(int page, int limit, string search, string filter, string sort, string date)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4", "5", "6" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                var query = _context.Coupons.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 20));
                    query = query.Where(c => c.CouponCode.Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(date))
                {
                    if (!DateTime.TryParse(date, out var selectedDate))
                        return new { errCode = 1, errMessage = "Tham số date không hợp lệ!", data = (object?)null };

                    selectedDate = selectedDate.Date;
                    var endOfDay = selectedDate.AddDays(1).AddTicks(-1);

                    query = query.Where(c => c.StartDate <= endOfDay && (c.EndDate == null || c.EndDate >= selectedDate));
                }

                if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];

                    if (field == "couponstatus")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("CouponStatus", value))
                            return new { errCode = 1, errMessage = "Trạng thái mã giảm giá không hợp lệ!", data = (object?)null };
                        query = query.Where(c => c.CouponStatus == value);
                    }
                    else if (field == "discounttype")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("DiscountType", value))
                            return new { errCode = 1, errMessage = "Loại giảm giá không hợp lệ!", data = (object?)null };
                        query = query.Where(c => c.DiscountType == value);
                    }
                    else if (field == "maxdiscountfixed")
                    {
                        query = query.Where(c => c.DiscountType == "FIXED");
                        switch (value)
                        {
                            case "0":
                                query = query.Where(c => c.MaxDiscount >= 0 && c.MaxDiscount <= 20000);
                                break;
                            case "1":
                                query = query.Where(c => c.MaxDiscount > 20000 && c.MaxDiscount <= 50000);
                                break;
                            case "2":
                                query = query.Where(c => c.MaxDiscount > 50000 && c.MaxDiscount <= 100000);
                                break;
                            case "3":
                                query = query.Where(c => c.MaxDiscount > 100000);
                                break;
                            default:
                                return new { errCode = 1, errMessage = "Khoảng giá không hợp lệ!", data = (object?)null };
                        }
                    }
                    else
                    {
                        return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };
                    }
                }

                query = sort switch
                {
                    "1" => query.OrderByDescending(c => c.CreatedAt),
                    "2" => query.OrderBy(c => c.CreatedAt),
                    "3" => query.OrderBy(c => c.EndDate),
                    "4" => query.OrderByDescending(c => c.EndDate),
                    "5" => query.OrderBy(c => c.MaxDiscount),
                    "6" => query.OrderByDescending(c => c.MaxDiscount),
                    _ => query.OrderByDescending(c => c.CreatedAt)
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.CouponID,
                        c.CouponCode,
                        c.CouponDescription,
                        c.MinOrderValue,
                        c.DiscountValue,
                        c.MaxDiscount,
                        c.DiscountType,
                        c.CouponStatus,
                        c.StartDate,
                        c.EndDate,
                        c.CreatedAt
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy mã giảm giá nào!", data = new List<object>(), totalItems = 0 };

                return new { errCode = 0, errMessage = "Lấy danh sách mã giảm giá thành công!", data = rows, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách mã giảm giá: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> CreateCouponAsync(CouponRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null)
                    return new { errCode = -1, errMessage = "Thiếu thông tin mã giảm giá!", data = (object?)null };

                var validationResult = await ValidateCouponInput(request);
                if (validationResult != null)
                    return validationResult;

                if (await CheckCouponCodeExistAsync(request.CouponCode))
                    return new { errCode = 1, errMessage = "Mã giảm giá đã tồn tại trong hệ thống!", data = (object?)null };

                var coupon = new Coupon
                {
                    CouponID = Guid.NewGuid().ToString(),
                    CouponCode = request.CouponCode.Trim(),
                    CouponDescription = string.IsNullOrEmpty(request.CouponDescription) ? null : request.CouponDescription.Trim(),
                    MinOrderValue = request.MinOrderValue,
                    DiscountValue = request.DiscountValue,
                    MaxDiscount = request.MaxDiscount,
                    DiscountType = request.DiscountType,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedAt = DateTime.Now,
                    CouponStatus = request.CouponStatus
                };

                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new { errCode = 0, errMessage = "Tạo mã giảm giá thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi tạo mã giảm giá: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeCouponInfoAsync(CouponRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CouponID))
                    return new { errCode = -1, errMessage = "Thiếu tham số hoặc couponid!", data = (object?)null };

                var validationResult = await ValidateCouponInput(request);
                if (validationResult != null)
                    return validationResult;

                var coupon = await _context.Coupons
                    .Where(c => c.CouponID == request.CouponID)
                    .FirstOrDefaultAsync();

                if (coupon == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Mã giảm giá không tồn tại!", data = (object?)null };
                }

                bool isUpdated = false;
                if (!string.IsNullOrEmpty(request.CouponCode) && request.CouponCode.Trim() != coupon.CouponCode)
                {
                    coupon.CouponCode = request.CouponCode.Trim();
                    isUpdated = true;
                }
                if (request.CouponDescription != null && request.CouponDescription.Trim() != (coupon.CouponDescription ?? ""))
                {
                    coupon.CouponDescription = string.IsNullOrEmpty(request.CouponDescription) ? null : request.CouponDescription.Trim();
                    isUpdated = true;
                }
                if (request.MinOrderValue != coupon.MinOrderValue)
                {
                    coupon.MinOrderValue = request.MinOrderValue;
                    isUpdated = true;
                }
                if (request.DiscountValue != coupon.DiscountValue)
                {
                    coupon.DiscountValue = request.DiscountValue;
                    isUpdated = true;
                }
                if (request.MaxDiscount != coupon.MaxDiscount)
                {
                    coupon.MaxDiscount = request.MaxDiscount;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.DiscountType) && request.DiscountType != coupon.DiscountType)
                {
                    coupon.DiscountType = request.DiscountType;
                    isUpdated = true;
                }
                if (request.StartDate != default && request.StartDate != coupon.StartDate)
                {
                    coupon.StartDate = request.StartDate;
                    isUpdated = true;
                }
                if (request.EndDate != coupon.EndDate)
                {
                    coupon.EndDate = request.EndDate;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.CouponStatus) && request.CouponStatus != coupon.CouponStatus)
                {
                    coupon.CouponStatus = request.CouponStatus;
                    isUpdated = true;
                }

                if (!isUpdated)
                    return new { errCode = 0, errMessage = "Không có thông tin nào để cập nhật!", data = (object?)null };

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật thông tin coupon thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật thông tin coupon: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> CheckCouponAsync(string couponCode, decimal price)
        {
            try
            {
                if (string.IsNullOrEmpty(couponCode) || price < 0)
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                decimal discountAmount = 0;
                var couponInfo = await _context.Coupons
                    .Where(c => c.CouponCode == couponCode && c.CouponStatus == "ACTIVE")
                    .Select(c => new
                    {
                        c.DiscountType,
                        c.DiscountValue,
                        c.MaxDiscount,
                        c.MinOrderValue,
                        c.StartDate,
                        c.EndDate
                    })
                    .FirstOrDefaultAsync();

                if (couponInfo == null)
                    return new { errCode = 1, errMessage = "Mã giảm giá không tồn tại hoặc đã hết hạn!", data = discountAmount };

                var currentDate = DateTime.Now;
                if (couponInfo.StartDate > currentDate || (couponInfo.EndDate.HasValue && couponInfo.EndDate < currentDate))
                    return new { errCode = 2, errMessage = "Mã giảm giá không trong thời gian hiệu lực!", data = discountAmount };

                if (price < (couponInfo.MinOrderValue ?? 0))
                    return new { errCode = 2, errMessage = "Không thể áp dụng mã giảm giá!", data = discountAmount };

                decimal discountValue;
                if (couponInfo.DiscountType == "FIXED")
                {
                    discountValue = couponInfo.DiscountValue;
                }
                else
                {
                    discountValue = price * (couponInfo.DiscountValue / 100);
                }

                discountValue = couponInfo.MaxDiscount.HasValue && discountValue > couponInfo.MaxDiscount.Value ? couponInfo.MaxDiscount.Value : discountValue;

                return new { errCode = 0, errMessage = "Kiểm tra giảm giá thành công!", data = discountValue };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi kiểm tra thông tin: {ex.Message}", data = (object?)null };
            }
        }
    }
}