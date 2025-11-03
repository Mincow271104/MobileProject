namespace be.Repositories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using be.Models;
    using be.Models.DTOs;

    public class BannerRepository : IBannerRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUtilitiesRepository _utilitiesRepository;

        public BannerRepository(ApplicationDbContext context, IUtilitiesRepository utilitiesRepository)
        {
            _context = context;
            _utilitiesRepository = utilitiesRepository;
        }

        private async Task<object> ValidateBannerInput(BannerRequest request)
        {
            if (request == null || request.GetType().GetProperties().All(p => p.GetValue(request) == null))
                return new { errCode = -1, errMessage = "Thiếu thông tin banner!", data = (object?)null };

            if (string.IsNullOrEmpty(request.BannerImage) || request.BannerImage.Trim().Length > 2048)
                return new { errCode = 1, errMessage = "Hình ảnh banner không hợp lệ hoặc vượt quá 2048 ký tự!", data = (object?)null };

            if (request.HiddenAt.HasValue)
            {
                var hiddenAtDate = request.HiddenAt.Value;
                if (hiddenAtDate <= DateTime.Now)
                    return new { errCode = 1, errMessage = "Ngày ẩn không hợp lệ hoặc phải lớn hơn thời gian hiện tại!", data = (object?)null };
            }

            if (string.IsNullOrEmpty(request.BannerStatus))
                return new { errCode = -1, errMessage = "Trạng thái banner không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("BannerStatus", request.BannerStatus))
                return new { errCode = 1, errMessage = $"Trạng thái banner {request.BannerStatus} không hợp lệ!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.ProductID))
            {
                var productExists = await _context.Products.AnyAsync(p => p.ProductID == request.ProductID);
                if (!productExists)
                    return new { errCode = 1, errMessage = $"Sản phẩm {request.ProductID} không tồn tại!", data = (object?)null };
            }

            return null;
        }

        private async Task<object> UpdateHideBannerAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var updated = await _context.Banners
                    .Where(b => b.BannerStatus == "SHOW" && b.HiddenAt != null && b.HiddenAt <= DateTime.Now)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.BannerStatus, "HIDE"));

                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật trạng thái banner thành công!", data = new { updatedCount = updated } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật trạng thái banner: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetBannerSaleInfoAsync(string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                await UpdateHideBannerAsync();

                if (productId == "ALL")
                {
                    var banners = await _context.Banners
                        .Where(b => b.BannerStatus == "SHOW")
                        .Join(_context.Products,
                            b => b.ProductID,
                            p => p.ProductID,
                            (b, p) => new
                            {
                                b.BannerID,
                                b.BannerImage,
                                b.ProductID,
                                ProductName = p.ProductName,
                                ProductImage = p.ProductImage
                            })
                        .ToListAsync();

                    if (!banners.Any())
                        return new { errCode = 1, errMessage = "Không tìm thấy banner nào!", data = new List<object>() };

                    return new { errCode = 0, errMessage = "Lấy thông tin banner thành công!", data = banners };
                }
                else
                {
                    var banner = await _context.Banners
                        .Where(b => b.ProductID == productId && b.BannerStatus == "SHOW")
                        .Join(_context.Products,
                            b => b.ProductID,
                            p => p.ProductID,
                            (b, p) => new
                            {
                                b.BannerID,
                                b.BannerImage,
                                b.ProductID,
                                ProductName = p.ProductName,
                                ProductImage = p.ProductImage
                            })
                        .FirstOrDefaultAsync();

                    if (banner == null)
                        return new { errCode = 2, errMessage = "Banner không tồn tại!", data = (object?)null };

                    return new { errCode = 0, errMessage = "Lấy thông tin banner thành công!", data = banner };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin banner: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetBannerInfoAsync(string bannerId)
        {
            try
            {
                if (string.IsNullOrEmpty(bannerId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var banner = await _context.Banners
                    .Where(b => b.BannerID == bannerId)
                    .Select(b => new
                    {
                        b.BannerID,
                        b.BannerImage,
                        b.CreatedAt,
                        HiddenAt = b.HiddenAt != null ? b.HiddenAt.Value.ToString("O") : null,
                        b.BannerStatus,
                        b.ProductID,
                        ProductName = _context.Products
                            .Where(p => p.ProductID == b.ProductID)
                            .Select(p => p.ProductName)
                            .FirstOrDefault(),
                        ProductType = _context.Products
                            .Where(p => p.ProductID == b.ProductID)
                            .Select(p => p.ProductType)
                            .FirstOrDefault(),
                        PetTypes = _context.ProductPetTypes
                            .Where(pt => pt.ProductID == b.ProductID)
                            .Select(pt => pt.PetType)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (banner == null)
                    return new { errCode = 2, errMessage = "Banner không tồn tại!", data = (object?)null };

                return new { errCode = 0, errMessage = "Lấy thông tin banner thành công!", data = banner };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin banner: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadBannerInfoAsync(int page, int limit, string search, string filter, string sort, string date)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                await UpdateHideBannerAsync();

                var query = _context.Banners.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 100));
                    var productIds = await _context.Products
                        .Where(p => p.ProductName.Contains(searchTerm))
                        .Select(p => p.ProductID)
                        .ToListAsync();

                    if (!productIds.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy banner nào!", data = new List<object>(), totalItems = 0 };

                    query = query.Where(b => productIds.Contains(b.ProductID));
                }

                if (!string.IsNullOrEmpty(date))
                {
                    if (!DateTime.TryParse(date, out var startOfDay))
                        return new { errCode = 1, errMessage = "Tham số date không hợp lệ!", data = (object?)null };

                    startOfDay = startOfDay.Date;
                    var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                    query = query.Where(b => b.CreatedAt <= endOfDay && (b.HiddenAt == null || b.HiddenAt >= startOfDay));
                }

                if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];

                    if (field == "bannerstatus")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("BannerStatus", value))
                            return new { errCode = 1, errMessage = "BannerStatus không hợp lệ!", data = (object?)null };

                        query = query.Where(b => b.BannerStatus == value);
                    }
                    else
                    {
                        return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };
                    }
                }

                query = sort switch
                {
                    "1" => query.OrderByDescending(b => b.CreatedAt),
                    "2" => query.OrderBy(b => b.CreatedAt),
                    "3" => query.OrderBy(b => b.HiddenAt == null).ThenBy(b => b.HiddenAt),
                    "4" => query.OrderByDescending(b => b.HiddenAt == null).ThenByDescending(b => b.HiddenAt),
                    _ => query.OrderByDescending(b => b.CreatedAt)
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(b => new
                    {
                        b.BannerID,
                        b.BannerImage,
                        b.CreatedAt,
                        HiddenAt = b.HiddenAt != null ? b.HiddenAt.Value.ToString("O") : null,
                        b.BannerStatus,
                        b.ProductID,
                        ProductName = _context.Products
                            .Where(p => p.ProductID == b.ProductID)
                            .Select(p => p.ProductName)
                            .FirstOrDefault() ?? "N/A",
                        ProductImage = _context.Products
                            .Where(p => p.ProductID == b.ProductID)
                            .Select(p => p.ProductImage)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy banner nào!", data = new List<object>(), totalItems = 0 };

                return new { errCode = 0, errMessage = "Lấy danh sách banner thành công!", data = rows, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách banner: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> CreateBannerAsync(BannerRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null)
                    return new { errCode = -1, errMessage = "Thiếu thông tin banner!", data = (object?)null };

                var validationResult = await ValidateBannerInput(request);
                if (validationResult != null)
                    return validationResult;

                var banner = new Banner
                {
                    BannerID = Guid.NewGuid().ToString(),
                    BannerImage = request.BannerImage.Trim(),
                    CreatedAt = DateTime.Now,
                    HiddenAt = request.HiddenAt,
                    BannerStatus = request.BannerStatus,
                    ProductID = request.ProductID
                };

                _context.Banners.Add(banner);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new { errCode = 0, errMessage = "Tạo banner thành công!", data = new { BannerID = banner.BannerID } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi tạo banner: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeBannerInfoAsync(BannerRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.BannerID))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var validationResult = await ValidateBannerInput(request);
                if (validationResult != null)
                    return validationResult;

                var banner = await _context.Banners
                    .Where(b => b.BannerID == request.BannerID)
                    .FirstOrDefaultAsync();

                if (banner == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Banner không tồn tại!", data = (object?)null };
                }

                bool isUpdated = false;
                if (!string.IsNullOrEmpty(request.BannerImage) && request.BannerImage != banner.BannerImage)
                {
                    banner.BannerImage = request.BannerImage.Trim();
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.BannerStatus) && request.BannerStatus != banner.BannerStatus)
                {
                    banner.BannerStatus = request.BannerStatus;
                    isUpdated = true;
                }
                if (request.ProductID != banner.ProductID)
                {
                    banner.ProductID = request.ProductID;
                    isUpdated = true;
                }
                if (request.HiddenAt != banner.HiddenAt)
                {
                    banner.HiddenAt = request.HiddenAt;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Không có thông tin nào để cập nhật!", data = (object?)null };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật thông tin banner thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật banner: {ex.Message}", data = (object?)null };
            }
        }
    }
}