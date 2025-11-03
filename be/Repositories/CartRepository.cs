using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using be.Models;
using be.Models.DTOs;

namespace be.Repositories
{

    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetCartAsync(string accountId)
        {
            try
            {
                if (string.IsNullOrEmpty(accountId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var cartItems = await _context.CartItems
                    .Where(ci => ci.AccountID == accountId)
                    .Select(ci => new
                    {
                        ci.CartItemID,
                        ci.ProductID,
                        ci.ProductDetailID,
                        ci.ItemPrice,
                        ci.ItemQuantity
                    })
                    .ToListAsync();

                if (!cartItems.Any())
                    return new { errCode = 0, errMessage = "Giỏ hàng trống!", data = new List<object>() };

                return new { errCode = 0, errMessage = "Tải giỏ hàng thành công!", data = cartItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetCartDetailAsync(CartInfoRequest[] cartInfo)
        {
            try
            {
                if (cartInfo == null || !cartInfo.Any())
                    return new { errCode = -1, errMessage = "Thiếu hoặc sai định dạng tham số!", data = (object?)null };

                var cartDetailList = new List<object>();

                foreach (var item in cartInfo)
                {
                    if (string.IsNullOrEmpty(item.ProductID) || string.IsNullOrEmpty(item.ProductDetailID) || item.ItemQuantity == 0)
                    {
                        cartDetailList.Add(new { item.CartItemID, errCode = 1, errMessage = "Thông tin sản phẩm không hợp lệ!", data = (object?)null });
                        continue;
                    }

                    var product = await _context.Products
                        .Where(p => p.ProductID == item.ProductID)
                        .Select(p => new
                        {
                            p.ProductID,
                            p.ProductName,
                            p.ProductPrice,
                            p.ProductImage,
                            p.ProductType,
                            p.ProductDescription
                        })
                        .FirstOrDefaultAsync();

                    if (product == null)
                    {
                        cartDetailList.Add(new { item.CartItemID, errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null });
                        continue;
                    }

                    var productDetail = await _context.ProductDetails
                        .Where(pd => pd.ProductID == item.ProductID && pd.ProductDetailID == item.ProductDetailID && pd.DetailStatus == "AVAIL")
                        .Select(pd => new
                        {
                            pd.ProductDetailID,
                            pd.DetailName,
                            pd.Stock,
                            pd.SoldCount,
                            pd.ExtraPrice,
                            pd.Promotion
                        })
                        .FirstOrDefaultAsync();

                    if (productDetail == null)
                    {
                        cartDetailList.Add(new { item.CartItemID, errCode = 2, errMessage = "Chi tiết sản phẩm không tồn tại hoặc hết hàng!", data = (object?)null });
                        continue;
                    }

                    var finalItemQuantity = Math.Min(item.ItemQuantity, productDetail.Stock);
                    if (item.ItemQuantity > productDetail.Stock)
                    {
                        await _context.CartItems
                            .Where(ci => ci.CartItemID == item.CartItemID)
                            .ExecuteUpdateAsync(s => s.SetProperty(ci => ci.ItemQuantity, finalItemQuantity));
                    }

                    var finalProductPrice = product.ProductPrice + productDetail.ExtraPrice;
                    var finalPrice = finalProductPrice * (1 - productDetail.Promotion / 100);

                    cartDetailList.Add(new
                    {
                        CartItemID = item.CartItemID,
                        product.ProductID,
                        product.ProductName,
                        product.ProductImage,
                        product.ProductDescription,
                        productDetail.ProductDetailID,
                        productDetail.DetailName,
                        productDetail.Stock,
                        productDetail.Promotion,
                        ProductPrice = finalProductPrice,
                        ItemPrice = finalPrice,
                        ItemQuantity = finalItemQuantity,
                        TotalPrice = finalPrice * finalItemQuantity
                    });
                }

                if (cartDetailList.Any(item => item.GetType().GetProperty("errCode") != null))
                    return new { errCode = 1, errMessage = "Một số sản phẩm trong giỏ hàng không hợp lệ!", data = cartDetailList };

                return new { errCode = 0, errMessage = "Lấy thông tin chi tiết giỏ hàng thành công!", data = cartDetailList };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin chi tiết giỏ hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetDetailListAsync(CartInfoRequest[] cartInfo)
        {
            try
            {
                if (cartInfo == null || !cartInfo.Any())
                    return new { errCode = -1, errMessage = "Thiếu hoặc sai định dạng tham số!", data = (object?)null };

                var cartDetailList = new List<object>();

                foreach (var item in cartInfo)
                {
                    if (string.IsNullOrEmpty(item.ProductID))
                    {
                        cartDetailList.Add(new { ProductID = item.ProductID, errCode = 1, errMessage = "Mã sản phẩm không hợp lệ!", data = (object?)null });
                        continue;
                    }

                    var productDetails = await _context.ProductDetails
                        .Where(pd => pd.ProductID == item.ProductID && pd.DetailStatus == "AVAIL")
                        .Select(pd => new
                        {
                            ProductDetailID = pd.ProductDetailID,
                            DetailName = pd.DetailName,
                            Stock = pd.Stock
                        })
                        .ToListAsync();

                    var detailList = productDetails.Any()
                        ? productDetails.Select(pd => new { pd.ProductDetailID, pd.DetailName, pd.Stock }).ToList<object>()
                        : new List<object>();

                    cartDetailList.Add(new
                    {
                        ProductID = item.ProductID,
                        DetailList = detailList
                    });
                }

                return new { errCode = 0, errMessage = "Lấy danh sách chi tiết sản phẩm thành công!", data = cartDetailList };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> AddToCartAsync(AddToCartRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request == null || string.IsNullOrEmpty(request.AccountID) || request.CartInfo == null || !request.CartInfo.Any())
                    return new { errCode = -1, errMessage = "Thiếu hoặc sai định dạng tham số!", data = (object?)null };

                var account = await _context.Accounts
                    .Where(a => a.AccountID == request.AccountID)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Tài khoản không tồn tại!", data = (object?)null };
                }

                foreach (var item in request.CartInfo)
                {
                    if (string.IsNullOrEmpty(item.ProductID) || string.IsNullOrEmpty(item.ProductDetailID) || item.ItemPrice < 0 || item.ItemQuantity <= 0)
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = "Thông tin sản phẩm không hợp lệ!", data = (object?)null };
                    }

                    var productDetail = await _context.ProductDetails
                        .Where(pd => pd.ProductID == item.ProductID && pd.ProductDetailID == item.ProductDetailID && pd.DetailStatus == "AVAIL")
                        .Select(pd => new { pd.Stock })
                        .FirstOrDefaultAsync();

                    if (productDetail == null)
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = $"Chi tiết sản phẩm {item.ProductID}-{item.ProductDetailID} không tồn tại hoặc hết hàng!", data = (object?)null };
                    }

                    if (item.ItemQuantity > productDetail.Stock)
                    {
                        await transaction.RollbackAsync();
                        return new { errCode = 1, errMessage = $"Số lượng yêu cầu vượt quá tồn kho cho sản phẩm {item.ProductID}!", data = (object?)null };
                    }

                    var cartItem = await _context.CartItems
                        .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == item.ProductID && ci.ProductDetailID == item.ProductDetailID)
                        .FirstOrDefaultAsync();

                    if (cartItem != null)
                    {
                        cartItem.ItemQuantity += item.ItemQuantity;
                        cartItem.ItemPrice = item.ItemPrice;
                    }
                    else
                    {
                        _context.CartItems.Add(new CartItem
                        {
                            CartItemID = Guid.NewGuid().ToString(),
                            AccountID = request.AccountID,
                            ProductID = item.ProductID,
                            ProductDetailID = item.ProductDetailID,
                            ItemPrice = item.ItemPrice,
                            ItemQuantity = item.ItemQuantity
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Thêm vào giỏ hàng thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi thêm vào giỏ hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> UpdateQuantityAsync(UpdateQuantityRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(request.AccountID) || string.IsNullOrEmpty(request.ProductID) || string.IsNullOrEmpty(request.ProductDetailID) || request.Quantity < 0)
                    return new { errCode = -1, errMessage = "Thiếu tham số hoặc số lượng không hợp lệ!", data = (object?)null };

                var cartItem = await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID)
                    .FirstOrDefaultAsync();

                if (cartItem == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại trong giỏ hàng!", data = (object?)null };
                }

                var productDetail = await _context.ProductDetails
                    .Where(pd => pd.ProductID == request.ProductID && pd.ProductDetailID == request.ProductDetailID && pd.DetailStatus == "AVAIL")
                    .Select(pd => new { pd.Stock })
                    .FirstOrDefaultAsync();

                if (productDetail == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Chi tiết sản phẩm không tồn tại hoặc hết hàng!", data = (object?)null };
                }

                if (request.Quantity > productDetail.Stock)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 1, errMessage = "Số lượng yêu cầu vượt quá tồn kho!", data = (object?)null };
                }

                if (request.Quantity == 0)
                {
                    await _context.CartItems
                        .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID)
                        .ExecuteDeleteAsync();
                }
                else
                {
                    cartItem.ItemQuantity = request.Quantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = request.Quantity == 0 ? "Xóa sản phẩm khỏi giỏ hàng thành công!" : "Cập nhật số lượng thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> UpdateCartDetailAsync(UpdateCartDetailRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(request.AccountID) || string.IsNullOrEmpty(request.ProductID) || string.IsNullOrEmpty(request.ProductDetailID1) || string.IsNullOrEmpty(request.ProductDetailID2))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var cartItem = await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID1)
                    .FirstOrDefaultAsync();

                if (cartItem == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại trong giỏ hàng!", data = (object?)null };
                }

                var newDetail = await _context.ProductDetails
                    .Where(pd => pd.ProductID == request.ProductID && pd.ProductDetailID == request.ProductDetailID2 && pd.DetailStatus == "AVAIL")
                    .Select(pd => new { pd.Stock, pd.ExtraPrice, pd.Promotion })
                    .FirstOrDefaultAsync();

                if (newDetail == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Chi tiết sản phẩm mới không tồn tại hoặc hết hàng!", data = (object?)null };
                }

                var product = await _context.Products
                    .Where(p => p.ProductID == request.ProductID)
                    .Select(p => new { p.ProductPrice })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null };
                }

                var newItemQuantity = Math.Min(cartItem.ItemQuantity, newDetail.Stock);
                if (cartItem.ItemQuantity > newDetail.Stock)
                    cartItem.ItemQuantity = newDetail.Stock;

                var newItemPrice = (product.ProductPrice + newDetail.ExtraPrice) * (1 - newDetail.Promotion / 100);
                cartItem.ProductDetailID = request.ProductDetailID2;
                cartItem.ItemPrice = newItemPrice;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Cập nhật chi tiết giỏ hàng thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> MergeCartDetailAsync(MergeCartDetailRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(request.AccountID) || string.IsNullOrEmpty(request.ProductID) || string.IsNullOrEmpty(request.ProductDetailID1) || string.IsNullOrEmpty(request.ProductDetailID2) || request.Quantity < 0)
                    return new { errCode = -1, errMessage = "Thiếu tham số hoặc số lượng không hợp lệ!", data = (object?)null };

                var detail1 = await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID1)
                    .Select(ci => new { ci.CartItemID, ci.ItemQuantity, ci.ItemPrice })
                    .FirstOrDefaultAsync();

                var detail2 = await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID2)
                    .Select(ci => new { ci.CartItemID, ci.ItemQuantity, ci.ItemPrice })
                    .FirstOrDefaultAsync();

                var productDetail = await _context.ProductDetails
                    .Where(pd => pd.ProductID == request.ProductID && pd.ProductDetailID == request.ProductDetailID2 && pd.DetailStatus == "AVAIL")
                    .Select(pd => new { pd.ProductDetailID, pd.Stock, pd.ExtraPrice, pd.Promotion })
                    .FirstOrDefaultAsync();

                if (productDetail == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Chi tiết sản phẩm không tồn tại hoặc hết hàng!", data = (object?)null };
                }

                var product = await _context.Products
                    .Where(p => p.ProductID == request.ProductID)
                    .Select(p => new { p.ProductPrice })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại!", data = (object?)null };
                }

                var newItemPrice = (product.ProductPrice + productDetail.ExtraPrice) * (1 - productDetail.Promotion / 100);
                var totalQuantity = Math.Min(request.Quantity, productDetail.Stock);

                if (detail1 == null && detail2 == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Không tìm thấy sản phẩm trong giỏ hàng!", data = (object?)null };
                }

                if (detail2 != null)
                {
                    await _context.CartItems
                        .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID2)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(ci => ci.ItemQuantity, totalQuantity)
                            .SetProperty(ci => ci.ItemPrice, newItemPrice));
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CartItemID = Guid.NewGuid().ToString(),
                        AccountID = request.AccountID,
                        ProductID = request.ProductID,
                        ProductDetailID = request.ProductDetailID2,
                        ItemPrice = newItemPrice,
                        ItemQuantity = totalQuantity
                    });
                }

                if (detail1 != null)
                {
                    await _context.CartItems
                        .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID1)
                        .ExecuteDeleteAsync();
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Gộp chi tiết sản phẩm thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi gộp chi tiết sản phẩm: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> RemoveFromCartAsync(RemoveFromCartRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(request.AccountID) || string.IsNullOrEmpty(request.ProductID) || string.IsNullOrEmpty(request.ProductDetailID))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var cartItem = await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID)
                    .FirstOrDefaultAsync();

                if (cartItem == null)
                {
                    await transaction.RollbackAsync();
                    return new { errCode = 2, errMessage = "Sản phẩm không tồn tại trong giỏ hàng!", data = (object?)null };
                }

                await _context.CartItems
                    .Where(ci => ci.AccountID == request.AccountID && ci.ProductID == request.ProductID && ci.ProductDetailID == request.ProductDetailID)
                    .ExecuteDeleteAsync();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Xóa sản phẩm khỏi giỏ hàng thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin: {ex.Message}", data = (object?)null };
            }
        }
    }
}