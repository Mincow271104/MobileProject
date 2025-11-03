    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using be.Models;
    using be.Models.DTOs;
namespace be.Repositories
{


    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUtilitiesRepository _utilitiesRepository;

        public ProductRepository(ApplicationDbContext context, IUtilitiesRepository utilitiesRepository)
        {
            _context = context;
            _utilitiesRepository = utilitiesRepository;
        }

        private async Task<object> ValidateProductInput(CreateProductRequest request)
        {
            if (request == null || request.GetType().GetProperties().All(p => p.GetValue(request) == null))
                return new { errCode = -1, errMessage = "Thiếu thông tin sản phẩm!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ProductName))
                return new { errCode = -1, errMessage = "Tên sản phẩm không được để trống!", data = (object?)null };
            if (!Regex.IsMatch(request.ProductName.Trim(), @"^[A-Za-zÀ-ỹ0-9\s]{2,100}$"))
                return new { errCode = 1, errMessage = "Tên sản phẩm không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ProductType))
                return new { errCode = -1, errMessage = "Loại sản phẩm không được để trống!", data = (object?)null };
            if (!await _utilitiesRepository.CheckValidAllCodeAsync("ProductType", request.ProductType))
                return new { errCode = 1, errMessage = "Loại sản phẩm không hợp lệ!", data = (object?)null };

            if (request.ProductPrice <= 0)
                return new { errCode = -1, errMessage = "Giá sản phẩm phải lớn hơn 0!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.ProductDescription) && request.ProductDescription.Trim().Length > 65535)
                return new { errCode = 1, errMessage = "Mô tả sản phẩm không hợp lệ hoặc vượt quá giới hạn ký tự!", data = (object?)null };

            if (request.PetType == null || !request.PetType.Any())
                return new { errCode = -1, errMessage = "Vui lòng chọn ít nhất một loại thú cưng!", data = (object?)null };

            foreach (var petType in request.PetType)
            {
                if (!await _utilitiesRepository.CheckValidAllCodeAsync("PetType", petType))
                    return new { errCode = 1, errMessage = $"Loại thú cưng {petType} không hợp lệ!", data = (object?)null };
            }

            if (request.ProductDetail == null || !request.ProductDetail.Any())
                return new { errCode = -1, errMessage = "Vui lòng thêm ít nhất một chi tiết sản phẩm!", data = (object?)null };

            for (int i = 0; i < request.ProductDetail.Length; i++)
            {
                var detail = request.ProductDetail[i];
                if (string.IsNullOrEmpty(detail.DetailName))
                    return new { errCode = -1, errMessage = $"Tên chi tiết tại dòng {i + 1} không được để trống!", data = (object?)null };
                if (!Regex.IsMatch(detail.DetailName.Trim(), @"^[A-Za-zÀ-ỹ0-9\s]{2,50}$"))
                    return new { errCode = 1, errMessage = $"Tên chi tiết tại dòng {i + 1} không hợp lệ!", data = (object?)null };
                if (detail.Stock < 0)
                    return new { errCode = 1, errMessage = $"Số lượng tồn tại dòng {i + 1} phải lớn hơn hoặc bằng 0!", data = (object?)null };
                if (detail.ExtraPrice < 0)
                    return new { errCode = -1, errMessage = $"Giá thêm tại dòng {i + 1} phải lớn hơn hoặc bằng 0!", data = (object?)null };
                if (detail.Promotion < 0 || detail.Promotion > 100)
                    return new { errCode = 1, errMessage = $"Khuyến mãi tại dòng {i + 1} phải từ 0 đến 100%!", data = (object?)null };
                if (string.IsNullOrEmpty(detail.DetailStatus))
                    return new { errCode = -1, errMessage = $"Trạng thái chi tiết tại dòng {i + 1} không được để trống!", data = (object?)null };
                if (!await _utilitiesRepository.CheckValidAllCodeAsync("DetailStatus", detail.DetailStatus))
                    return new { errCode = 1, errMessage = $"Trạng thái chi tiết tại dòng {i + 1} không hợp lệ!", data = (object?)null };
                if (detail.Stock == 0 && detail.DetailStatus == "AVAIL")
                    return new { errCode = 1, errMessage = $"Số lượng tồn tại dòng {i + 1} bằng 0, không thể chọn trạng thái Còn hàng!", data = (object?)null };
            }

            if (request.Image != null && request.Image.Any())
            {
                if (request.Image.Any(img => string.IsNullOrEmpty(img.Image) || img.Image.Trim().Length > 2048))
                    return new { errCode = 1, errMessage = "Danh sách ảnh phụ không hợp lệ hoặc vượt quá 2048 ký tự!", data = (object?)null };
            }

            return null;
        }

        private async Task<bool> CheckProductNameExist(string productName, string excludeProductId = null)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(excludeProductId))
                query = query.Where(p => p.ProductID != excludeProductId);
            return await query.AnyAsync(p => p.ProductName == productName.Trim());
        }

        private async Task<object> UpdateOutOfStockAsync(string productId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var query = _context.ProductDetails.Where(pd => pd.Stock == 0);
                if (!string.IsNullOrEmpty(productId))
                    query = query.Where(pd => pd.ProductID == productId);

                var updated = await query
                    .ExecuteUpdateAsync(s => s.SetProperty(pd => pd.DetailStatus, "OUT"));

                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật trạng thái chi tiết sản phẩm thành công!", data = new { updatedCount = updated } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật trạng thái chi tiết sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetSaleProductInfoAsync(string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                await UpdateOutOfStockAsync();

                if (productId == "ALL")
                {
                    var products = await _context.Products
                        .Select(p => new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType
                        })
                        .ToListAsync();

                    if (!products.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>() };

                    var data = await Task.WhenAll(products.Select(async p =>
                    {
                        var petTypes = await _context.ProductPetTypes
                            .Where(pt => pt.ProductID == p.ProductID)
                            .Select(pt => pt.PetType)
                            .ToListAsync();

                        var details = await _context.ProductDetails
                            .Where(pd => pd.ProductID == p.ProductID && pd.DetailStatus == "AVAIL")
                            .Select(pd => new
                            {
                                pd.ProductDetailID,
                                pd.DetailName,
                                pd.Stock,
                                pd.SoldCount,
                                pd.ExtraPrice,
                                pd.Promotion
                            })
                            .ToListAsync();

                        var images = await _context.Images
                            .Where(img => img.ReferenceType == "Product" && img.ReferenceID == p.ProductID)
                            .Select(img => new { img.ImageID, img.ImageUrl })
                            .ToListAsync();

                        return new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType,
                            PetType = petTypes,
                            ProductDetail = details,
                            Image = images
                        };
                    }));

                    return new { errCode = 0, errMessage = "Lấy thông tin sản phẩm thành công!", data };
                }
                else
                {
                    var product = await _context.Products
                        .Where(p => p.ProductID == productId)
                        .Select(p => new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType
                        })
                        .FirstOrDefaultAsync();

                    if (product == null)
                        return new { errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null };

                    var petTypes = await _context.ProductPetTypes
                        .Where(pt => pt.ProductID == productId)
                        .Select(pt => pt.PetType)
                        .ToListAsync();

                    var details = await _context.ProductDetails
                        .Where(pd => pd.ProductID == productId && pd.DetailStatus == "AVAIL")
                        .Select(pd => new
                        {
                            pd.ProductDetailID,
                            pd.DetailName,
                            pd.Stock,
                            pd.SoldCount,
                            pd.ExtraPrice,
                            pd.Promotion
                        })
                        .ToListAsync();

                    var images = await _context.Images
                        .Where(img => img.ReferenceType == "Product" && img.ReferenceID == productId)
                        .Select(img => new { img.ImageID, img.ImageUrl })
                        .ToListAsync();

                    var data = new
                    {
                        product.ProductID,
                        product.ProductName,
                        product.ProductDescription,
                        product.ProductPrice,
                        product.ProductImage,
                        product.ProductType,
                        PetType = petTypes,
                        ProductDetail = details,
                        Image = images
                    };

                    return new { errCode = 0, errMessage = "Lấy thông tin sản phẩm thành công!", data };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadSaleProductInfoAsync(int page, int limit, string search, string filter, string sort)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && filter != "PROMOTION" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                await UpdateOutOfStockAsync();

                var query = _context.Products.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 100));
                    query = query.Where(p => p.ProductName.Contains(searchTerm));
                }

                if (filter == "PROMOTION")
                {
                    var productId = await _context.ProductDetails
                        .Where(pd => pd.Promotion > 0 && pd.Stock > 0 && pd.DetailStatus == "AVAIL")
                        .Select(pd => pd.ProductID)
                        .Distinct()
                        .ToListAsync();

                    if (!productId.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm khuyến mãi!", data = new List<object>(), totalItems = 0 };

                    query = query.Where(p => productId.Contains(p.ProductID));
                }
                else if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];

                    if (field == "producttype")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("ProductType", value))
                            return new { errCode = 1, errMessage = "Loại sản phẩm không hợp lệ!", data = (object?)null };
                        query = query.Where(p => p.ProductType == value);
                    }
                    else if (field == "pettype")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("PetType", value))
                            return new { errCode = 1, errMessage = "Loại thú cưng không hợp lệ!", data = (object?)null };

                        var productId = await _context.ProductPetTypes
                            .Where(pt => pt.PetType == value)
                            .Select(pt => pt.ProductID)
                            .ToListAsync();

                        if (!productId.Any())
                            return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>(), totalItems = 0 };

                        query = query.Where(p => productId.Contains(p.ProductID));
                    }
                    else
                    {
                        return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };
                    }
                }

                query = sort switch
                {
                    "1" => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Sum(pd => pd.SoldCount)),
                    "2" => query.OrderBy(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID && pd.Stock > 0 && pd.DetailStatus == "AVAIL").Min(pd => (p.ProductPrice + pd.ExtraPrice) * (1 - pd.Promotion / 100))),
                    "3" => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID && pd.Stock > 0 && pd.DetailStatus == "AVAIL").Min(pd => (p.ProductPrice + pd.ExtraPrice) * (1 - pd.Promotion / 100))),
                    "4" => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Max(pd => pd.CreatedAt)),
                    _ => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Max(pd => pd.CreatedAt))
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.ProductPrice,
                        p.ProductImage
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>(), totalItems = 0 };

                var productIds = rows.Select(p => p.ProductID).ToList();
                var details = await _context.ProductDetails
                    .Where(pd => productIds.Contains(pd.ProductID) && pd.DetailStatus == "AVAIL" && pd.Stock > 0)
                    .OrderBy(pd => pd.ProductDetailID)
                    .Select(pd => new { pd.ProductID, pd.ProductDetailID, pd.DetailName, pd.Stock, pd.ExtraPrice, pd.Promotion })
                    .ToListAsync();

                var detailMap = details.GroupBy(d => d.ProductID)
                    .ToDictionary(g => g.Key, g => new { defaultDetail = g.FirstOrDefault() });

                var data = rows
                    .Select(item =>
                    {
                        var detail = detailMap.ContainsKey(item.ProductID) ? detailMap[item.ProductID].defaultDetail : null;
                        if (detail == null)
                            return null;
                        var price = (item.ProductPrice + detail.ExtraPrice) * (1 - detail.Promotion / 100);
                        return new
                        {
                            item.ProductID,
                            item.ProductName,
                            ItemPrice = price.ToString("F2"),
                            item.ProductImage,
                            detail.ProductDetailID,
                            detail.DetailName,
                            detail.Promotion
                        };
                    })
                    .Where(item => item != null)
                    .ToList();

                return new { errCode = 0, errMessage = "Lấy danh sách sản phẩm thành công!", data, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetProductInfoAsync(string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                await UpdateOutOfStockAsync();

                if (productId == "ALL")
                {
                    var products = await _context.Products
                        .Select(p => new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType
                        })
                        .ToListAsync();

                    if (!products.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>() };

                    var data = await Task.WhenAll(products.Select(async p =>
                    {
                        var petTypes = await _context.ProductPetTypes
                            .Where(pt => pt.ProductID == p.ProductID)
                            .Select(pt => pt.PetType)
                            .ToListAsync();

                        var details = await _context.ProductDetails
                            .Where(pd => pd.ProductID == p.ProductID)
                            .Select(pd => new
                            {
                                pd.ProductDetailID,
                                pd.DetailName,
                                pd.Stock,
                                pd.SoldCount,
                                pd.ExtraPrice,
                                pd.Promotion,
                                pd.DetailStatus
                            })
                            .ToListAsync();

                        var images = await _context.Images
                            .Where(img => img.ReferenceType == "Product" && img.ReferenceID == p.ProductID)
                            .Select(img => new { img.ImageID, img.ImageUrl })
                            .ToListAsync();

                        return new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType,
                            PetType = petTypes,
                            ProductDetail = details,
                            Image = images
                        };
                    }));

                    return new { errCode = 0, errMessage = "Lấy thông tin sản phẩm thành công!", data };
                }
                else
                {
                    var product = await _context.Products
                        .Where(p => p.ProductID == productId)
                        .Select(p => new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductDescription,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType
                        })
                        .FirstOrDefaultAsync();

                    if (product == null)
                        return new { errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null };

                    var petTypes = await _context.ProductPetTypes
                        .Where(pt => pt.ProductID == productId)
                        .Select(pt => pt.PetType)
                        .ToListAsync();

                    var details = await _context.ProductDetails
                        .Where(pd => pd.ProductID == productId)
                        .Select(pd => new
                        {
                            pd.ProductDetailID,
                            pd.DetailName,
                            pd.Stock,
                            pd.SoldCount,
                            pd.ExtraPrice,
                            pd.Promotion,
                            pd.DetailStatus
                        })
                        .ToListAsync();

                    var images = await _context.Images
                        .Where(img => img.ReferenceType == "Product" && img.ReferenceID == productId)
                        .Select(img => new { img.ImageID, img.ImageUrl })
                        .ToListAsync();

                    var data = new
                    {
                        product.ProductID,
                        product.ProductName,
                        product.ProductDescription,
                        product.ProductPrice,
                        product.ProductImage,
                        product.ProductType,
                        PetType = petTypes,
                        ProductDetail = details,
                        Image = images
                    };

                    return new { errCode = 0, errMessage = "Lấy thông tin sản phẩm thành công!", data };
                }
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadProductInfoAsync(int page, int limit, string search, string filter, string sort)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && filter != "PROMOTION" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                await UpdateOutOfStockAsync();

                var query = _context.Products.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 100));
                    query = query.Where(p => p.ProductName.Contains(searchTerm));
                }

                if (filter == "PROMOTION")
                {
                    var productId = await _context.ProductDetails
                        .Where(pd => pd.Promotion > 0)
                        .Select(pd => pd.ProductID)
                        .Distinct()
                        .ToListAsync();

                    if (!productId.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm khuyến mãi!", data = new List<object>(), totalItems = 0 };

                    query = query.Where(p => productId.Contains(p.ProductID));
                }
                else if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];

                    if (field == "producttype")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("ProductType", value))
                            return new { errCode = 1, errMessage = "Loại sản phẩm không hợp lệ!", data = (object?)null };
                        query = query.Where(p => p.ProductType == value);
                    }
                    else if (field == "pettype")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("PetType", value))
                            return new { errCode = 1, errMessage = "Loại thú cưng không hợp lệ!", data = (object?)null };

                        var productId = await _context.ProductPetTypes
                            .Where(pt => pt.PetType == value)
                            .Select(pt => pt.ProductID)
                            .ToListAsync();

                        if (!productId.Any())
                            return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>(), totalItems = 0 };

                        query = query.Where(p => productId.Contains(p.ProductID));
                    }
                    else
                    {
                        return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };
                    }
                }

                query = sort switch
                {
                    "1" => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Sum(pd => pd.SoldCount)),
                    "2" => query.OrderBy(p => p.ProductPrice),
                    "3" => query.OrderByDescending(p => p.ProductPrice),
                    "4" => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Max(pd => pd.CreatedAt)),
                    _ => query.OrderByDescending(p => _context.ProductDetails.Where(pd => pd.ProductID == p.ProductID).Max(pd => pd.CreatedAt))
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.ProductType,
                        p.ProductPrice,
                        p.ProductImage
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>(), totalItems = 0 };

                var productIds = rows.Select(p => p.ProductID).ToList();
                var stockData = await _context.ProductDetails
                    .Where(pd => productIds.Contains(pd.ProductID))
                    .GroupBy(pd => pd.ProductID)
                    .Select(g => new { ProductID = g.Key, TotalStock = g.Sum(pd => pd.Stock) })
                    .ToListAsync();

                var soldData = await _context.ProductDetails
                    .Where(pd => productIds.Contains(pd.ProductID))
                    .GroupBy(pd => pd.ProductID)
                    .Select(g => new { ProductID = g.Key, TotalSold = g.Sum(pd => pd.SoldCount) })
                    .ToListAsync();

                var stockMap = stockData.ToDictionary(s => s.ProductID, s => s.TotalStock);
                var soldMap = soldData.ToDictionary(s => s.ProductID, s => s.TotalSold);

                var data = rows.Select(item => new
                {
                    item.ProductID,
                    item.ProductName,
                    item.ProductType,
                    ProductPrice = item.ProductPrice,
                    item.ProductImage,
                    TotalStock = stockMap.ContainsKey(item.ProductID) ? stockMap[item.ProductID] : 0,
                    TotalSold = soldMap.ContainsKey(item.ProductID) ? soldMap[item.ProductID] : 0
                }).ToList();

                return new { errCode = 0, errMessage = "Lấy danh sách sản phẩm thành công!", data, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetProductDetailInfoAsync(string productId, string productDetailId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(productDetailId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                await UpdateOutOfStockAsync();

                var product = await _context.Products
                    .Where(p => p.ProductID == productId)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.ProductType,
                        p.ProductPrice,
                        p.ProductImage
                    })
                    .FirstOrDefaultAsync();

                var detail = await _context.ProductDetails
                    .Where(pd => pd.ProductID == productId && pd.ProductDetailID == productDetailId)
                    .Select(pd => new
                    {
                        pd.ProductDetailID,
                        pd.DetailName,
                        pd.Stock,
                        pd.SoldCount,
                        pd.ExtraPrice,
                        pd.Promotion,
                        pd.CreatedAt,
                        pd.DetailStatus
                    })
                    .FirstOrDefaultAsync();

                if (product == null || detail == null)
                    return new { errCode = 2, errMessage = "Sản phẩm hoặc chi tiết sản phẩm không tồn tại!", data = (object?)null };

                var data = new
                {
                    product.ProductID,
                    product.ProductName,
                    product.ProductType,
                    product.ProductPrice,
                    product.ProductImage,
                    detail.ProductDetailID,
                    detail.DetailName,
                    detail.Stock,
                    detail.SoldCount,
                    detail.ExtraPrice,
                    detail.Promotion,
                    detail.CreatedAt,
                    detail.DetailStatus
                };

                return new { errCode = 0, errMessage = "Lấy thông tin chi tiết sản phẩm thành công!", data };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin chi tiết sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> CreateProductAsync(CreateProductRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null)
                    return new { errCode = -1, errMessage = "Thiếu thông tin sản phẩm!", data = (object?)null };

                var validationResult = await ValidateProductInput(request);
                if (validationResult != null)
                    return validationResult;

                if (await CheckProductNameExist(request.ProductName))
                    return new { errCode = 1, errMessage = "Tên sản phẩm đã tồn tại trong hệ thống!", data = (object?)null };

                var productId = await _utilitiesRepository.GenerateIDAsync("P", 9, "Products", "ProductID");
                var createdAt = DateTime.Now;

                var product = new Product
                {
                    ProductID = productId,
                    ProductType = request.ProductType,
                    ProductName = request.ProductName.Trim(),
                    ProductPrice = request.ProductPrice,
                    ProductImage = request.ProductImage,
                    ProductDescription = string.IsNullOrEmpty(request.ProductDescription) ? null : request.ProductDescription.Trim().Substring(0, Math.Min(request.ProductDescription.Length, 65535))
                };

                _context.Products.Add(product);

                foreach (var detail in request.ProductDetail)
                {
                    _context.ProductDetails.Add(new ProductDetail
                    {
                        ProductDetailID = await _utilitiesRepository.GenerateIDAsync("PD", 9, "ProductDetails", "ProductDetailID"),
                        DetailName = detail.DetailName.Trim(),
                        Stock = detail.Stock,
                        SoldCount = detail.SoldCount,
                        ExtraPrice = detail.ExtraPrice,
                        Promotion = detail.Promotion,
                        CreatedAt = createdAt,
                        DetailStatus = detail.DetailStatus,
                        ProductID = productId
                    });
                }

                foreach (var petType in request.PetType)
                {
                    _context.ProductPetTypes.Add(new ProductPetType
                    {
                        ProductID = productId,
                        PetType = petType
                    });
                }

                if (request.Image != null && request.Image.Any())
                {
                    foreach (var image in request.Image)
                    {
                        _context.Images.Add(new Image
                        {
                            //ImageID = await _utilitiesRepository.GenerateIDAsync("IMG", 9, "Image", "ImageID"),
                            ImageUrl = image.Image.Trim().Substring(0, Math.Min(image.Image.Length, 2048)),
                            ReferenceType = "Product",
                            ReferenceID = productId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Tạo sản phẩm thành công!", data = new { ProductID = productId } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi tạo sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeProductInfoAsync(ChangeProductInfoRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ProductID))
                    return new { errCode = -1, errMessage = "Thiếu tham số ProductID!", data = (object?)null };

                //var validationResult = await ValidateProductInput(request as CreateProductRequest);
                //if (validationResult != null)
                //    return validationResult;

                var product = await _context.Products
                    .Where(p => p.ProductID == request.ProductID)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null };
                }

                if (!string.IsNullOrEmpty(request.ProductName) && request.ProductName != product.ProductName)
                {
                    if (await CheckProductNameExist(request.ProductName, request.ProductID))
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = "Tên sản phẩm đã tồn tại trong hệ thống!", data = (object?)null };
                    }
                }

                bool isUpdated = false;
                if (!string.IsNullOrEmpty(request.ProductName))
                {
                    product.ProductName = request.ProductName.Trim();
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.ProductType))
                {
                    product.ProductType = request.ProductType;
                    isUpdated = true;
                }
                if (request.ProductPrice.HasValue)
                {
                    product.ProductPrice = request.ProductPrice.Value;
                    isUpdated = true;
                }
                if (request.ProductImage != null)
                {
                    product.ProductImage = request.ProductImage;
                    isUpdated = true;
                }
                if (request.ProductDescription != null)
                {
                    product.ProductDescription = string.IsNullOrEmpty(request.ProductDescription) ? null : request.ProductDescription.Trim().Substring(0, Math.Min(request.ProductDescription.Length, 65535));
                    isUpdated = true;
                }

                if (request.PetType != null && request.PetType.Any())
                {
                    await _context.ProductPetTypes
                        .Where(pt => pt.ProductID == request.ProductID)
                        .ExecuteDeleteAsync();

                    foreach (var petType in request.PetType)
                    {
                        _context.ProductPetTypes.Add(new ProductPetType
                        {
                            ProductID = request.ProductID,
                            PetType = petType
                        });
                    }
                    isUpdated = true;
                }

                if (request.ProductDetail != null && request.ProductDetail.Any())
                {
                    var createdAt = DateTime.Now;
                    foreach (var detail in request.ProductDetail)
                    {
                        if (!string.IsNullOrEmpty(detail.ProductDetailID))
                        {
                            var existingDetail = await _context.ProductDetails
                                .Where(pd => pd.ProductDetailID == detail.ProductDetailID && pd.ProductID == request.ProductID)
                                .FirstOrDefaultAsync();

                            if (existingDetail != null)
                            {
                                existingDetail.DetailName = detail.DetailName.Trim();
                                existingDetail.Stock = detail.Stock;
                                existingDetail.SoldCount = detail.SoldCount;
                                existingDetail.ExtraPrice = detail.ExtraPrice;
                                existingDetail.Promotion = detail.Promotion;
                                existingDetail.DetailStatus = detail.DetailStatus;
                                isUpdated = true;
                            }
                            else
                            {
                                _context.ProductDetails.Add(new ProductDetail
                                {
                                    ProductDetailID = await _utilitiesRepository.GenerateIDAsync("PD", 9, "ProductDetails", "ProductDetailID"),
                                    DetailName = detail.DetailName.Trim(),
                                    Stock = detail.Stock,
                                    SoldCount = detail.SoldCount,
                                    ExtraPrice = detail.ExtraPrice,
                                    Promotion = detail.Promotion,
                                    CreatedAt = createdAt,
                                    DetailStatus = detail.DetailStatus,
                                    ProductID = request.ProductID
                                });
                                isUpdated = true;
                            }
                        }
                    }
                }

                if (request.Image != null)
                {
                    await _context.Images
                        .Where(img => img.ReferenceType == "Product" && img.ReferenceID == request.ProductID)
                        .ExecuteDeleteAsync();

                    if (request.Image.Any())
                    {
                        foreach (var image in request.Image)
                        {
                            _context.Images.Add(new Image
                            {
                                //ImageID = await _utilitiesRepository.GenerateIDAsync("IMG", 9, "Images", "ImageID"),
                                ImageUrl = image.Image.Trim().Substring(0, Math.Min(image.Image.Length, 2048)),
                                ReferenceType = "Product",
                                ReferenceID = request.ProductID
                            });
                        }
                        isUpdated = true;
                    }
                }

                if (!isUpdated)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Không có thông tin nào để cập nhật!", data = (object?)null };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật thông tin sản phẩm thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi cập nhật thông tin sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadFilteredProductInfoAsync(string filterProductType, string[] filterPetType)
        {
            try
            {
                if (filterPetType == null || !filterPetType.Any())
                    filterPetType = new[] { "ALL" };

                if (!string.IsNullOrEmpty(filterProductType) && filterProductType != "ALL")
                {
                    if (!await _utilitiesRepository.CheckValidAllCodeAsync("ProductType", filterProductType))
                        return new { errCode = 1, errMessage = $"Loại sản phẩm {filterProductType} không hợp lệ!", data = (object?)null };
                }

                if (filterPetType[0] != "ALL")
                {
                    foreach (var petType in filterPetType)
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("PetType", petType))
                            return new { errCode = 1, errMessage = $"Loại thú cưng {petType} không hợp lệ!", data = (object?)null };
                    }
                }

                await UpdateOutOfStockAsync();

                var query = _context.Products.AsQueryable();
                if (!string.IsNullOrEmpty(filterProductType) && filterProductType != "ALL")
                    query = query.Where(p => p.ProductType == filterProductType);

                if (filterPetType[0] != "ALL")
                {
                    var productIds = await _context.ProductPetTypes
                        .Where(pt => filterPetType.Contains(pt.PetType))
                        .Select(pt => pt.ProductID)
                        .Distinct()
                        .ToListAsync();

                    if (!productIds.Any())
                        return new { errCode = 0, errMessage = "Không tìm thấy sản phẩm nào!", data = new List<object>() };

                    query = query.Where(p => productIds.Contains(p.ProductID));
                }

                var products = await query
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.ProductType,
                        p.ProductPrice,
                        p.ProductImage
                    })
                    .ToListAsync();

                return new { errCode = 0, errMessage = "Lấy danh sách sản phẩm thành công!", data = products };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách sản phẩm: {ex.Message}", data = (object?)null };
            }
        }
    }
}