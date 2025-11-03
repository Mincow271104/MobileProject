using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using be.Models;
using be.Models.DTOs;
using System.Net.Mail;

namespace be.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUtilitiesRepository _utilitiesRepository;

        public InvoiceRepository(ApplicationDbContext context, IUtilitiesRepository utilitiesRepository)
        {
            _context = context;
            _utilitiesRepository = utilitiesRepository;
        }

        private async Task<object> ValidateInvoiceInput(InvoiceRequest request)
        {
            if (request == null || request.GetType().GetProperties().All(p => p.GetValue(request) == null))
                return new { errCode = -1, errMessage = "Thiếu thông tin hóa đơn!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ReceiverName))
                return new { errCode = -1, errMessage = "Tên người nhận không được để trống!", data = (object?)null };

            if (!Regex.IsMatch(request.ReceiverName.Trim(), @"^[A-Za-zÀ-ỹ0-9\s]{2,50}$"))
                return new { errCode = 1, errMessage = "Tên người nhận hàng sai định dạng!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ReceiverPhone))
                return new { errCode = -1, errMessage = "Số điện thoại không được để trống!", data = (object?)null };

            if (!Regex.IsMatch(request.ReceiverPhone.Trim(), @"^[0-9]{10,11}$"))
                return new { errCode = 1, errMessage = "Số điện thoại nhận hàng không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ReceiverAddress) || request.ReceiverAddress.Trim().Length > 255)
                return new { errCode = -1, errMessage = "Địa chỉ nhận hàng trống hoặc vượt quá 255 ký tự!", data = (object?)null };

            if (request.CartItems == null || !request.CartItems.Any())
                return new { errCode = -1, errMessage = "Giỏ hàng trống hoặc không hợp lệ!", data = (object?)null };

            for (int i = 0; i < request.CartItems.Length; i++)
            {
                var item = request.CartItems[i];
                if (string.IsNullOrEmpty(item.ProductID) || string.IsNullOrEmpty(item.ProductDetailID) || item.ItemQuantity <= 0 || item.ItemPrice < 0)
                    return new { errCode = -1, errMessage = $"Thiếu thông tin sản phẩm tại dòng {i + 1}!", data = (object?)null };

                var product = await _context.Products.AnyAsync(p => p.ProductID == item.ProductID);
                if (!product)
                    return new { errCode = 2, errMessage = $"Sản phẩm {item.ProductID} tại dòng {i + 1} không tồn tại!", data = (object?)null };

                var productDetail = await _context.ProductDetails
                    .Where(pd => pd.ProductDetailID == item.ProductDetailID)
                    .Select(pd => new { pd.Stock, pd.DetailName })
                    .FirstOrDefaultAsync();

                if (productDetail == null)
                    return new { errCode = 2, errMessage = $"Chi tiết sản phẩm {item.ProductDetailID} tại dòng {i + 1} không tồn tại!", data = (object?)null };

                if (productDetail.Stock < item.ItemQuantity)
                    return new { errCode = 2, errMessage = $"Sản phẩm tại dòng {i + 1} không đủ tồn kho!", data = (object?)null };
            }

            if (request.TotalQuantity <= 0)
                return new { errCode = -1, errMessage = "Tổng số lượng sản phẩm không hợp lệ!", data = (object?)null };

            if (request.TotalPrice < 0)
                return new { errCode = -1, errMessage = "Tổng giá trị đơn hàng không hợp lệ!", data = (object?)null };

            if (request.DiscountAmount < 0)
                return new { errCode = -1, errMessage = "Số tiền giảm giá không hợp lệ!", data = (object?)null };

            if (request.TotalPayment < 0)
                return new { errCode = -1, errMessage = "Tổng thanh toán không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.PaymentStatus))
                return new { errCode = -1, errMessage = "Trạng thái thanh toán không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("PaymentStatus", request.PaymentStatus))
                return new { errCode = 1, errMessage = "Trạng thái thanh toán không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ShippingStatus))
                return new { errCode = -1, errMessage = "Trạng thái giao hàng không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("ShippingStatus", request.ShippingStatus))
                return new { errCode = 1, errMessage = "Trạng thái giao hàng không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.PaymentType))
                return new { errCode = -1, errMessage = "Phương thức thanh toán không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("PaymentType", request.PaymentType))
                return new { errCode = 1, errMessage = "Phương thức thanh toán không hợp lệ!", data = (object?)null };

            if (string.IsNullOrEmpty(request.ShippingMethod))
                return new { errCode = -1, errMessage = "Phương thức giao hàng không được để trống!", data = (object?)null };

            if (!await _utilitiesRepository.CheckValidAllCodeAsync("ShippingMethod", request.ShippingMethod))
                return new { errCode = 1, errMessage = "Phương thức giao hàng không hợp lệ!", data = (object?)null };

            if (!string.IsNullOrEmpty(request.AccountID))
            {
                var accountExists = await _context.Accounts.AnyAsync(a => a.AccountID == request.AccountID);
                if (!accountExists)
                    return new { errCode = 1, errMessage = "Tài khoản không tồn tại trong hệ thống!", data = (object?)null };
            }

            if (!string.IsNullOrEmpty(request.CouponID))
            {
                var couponExists = await _context.Coupons.AnyAsync(c => c.CouponID == request.CouponID);
                if (!couponExists)
                    return new { errCode = 1, errMessage = "Mã giảm giá không tồn tại trong hệ thống!", data = (object?)null };
            }

            return null;
        }

        private async Task<bool> SendInvoiceEmailAsync(string invoiceId, string email)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Where(i => i.InvoiceID == invoiceId)
                    .Select(i => new
                    {
                        i.InvoiceID,
                        i.ReceiverName,
                        i.ReceiverPhone,
                        i.ReceiverAddress,
                        i.TotalQuantity,
                        i.TotalPrice,
                        i.DiscountAmount,
                        i.TotalPayment,
                        i.CreatedAt,
                        i.PaymentType,
                        i.ShippingMethod,
                        i.ShippingStatus
                    })
                    .FirstOrDefaultAsync();

                if (invoice == null)
                    return false;

                var invoiceDetails = await _context.InvoiceDetails
                    .Where(id => id.InvoiceID == invoiceId)
                    .Select(id => new { id.ProductID, id.ProductDetailID, id.ItemQuantity, id.ItemPrice })
                    .ToListAsync();

                var productList = new List<string>();
                foreach (var detail in invoiceDetails)
                {
                    var product = await _context.Products
                        .Where(p => p.ProductID == detail.ProductID)
                        .Select(p => p.ProductName)
                        .FirstOrDefaultAsync();

                    var productDetail = await _context.ProductDetails
                        .Where(pd => pd.ProductDetailID == detail.ProductDetailID)
                        .Select(pd => pd.DetailName)
                        .FirstOrDefaultAsync();

                    if (product != null && productDetail != null)
                        productList.Add($"- {product} ({productDetail}): {detail.ItemQuantity} x {detail.ItemPrice:F0} VND");
                }

                var paymentType = await _context.AllCodes
                    .Where(ac => ac.Type == "PaymentType" && ac.Code == invoice.PaymentType)
                    .Select(ac => ac.CodeValueVI)
                    .FirstOrDefaultAsync();

                var shippingMethod = await _context.AllCodes
                    .Where(ac => ac.Type == "ShippingMethod" && ac.Code == invoice.ShippingMethod)
                    .Select(ac => ac.CodeValueVI)
                    .FirstOrDefaultAsync();

                var shippingStatus = await _context.AllCodes
                    .Where(ac => ac.Type == "ShippingStatus" && ac.Code == invoice.ShippingStatus)
                    .Select(ac => ac.CodeValueVI)
                    .FirstOrDefaultAsync();

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("your_email@gmail.com", "your_email_password"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your_email@gmail.com"),
                    Subject = $"Hóa đơn #{invoice.InvoiceID} - Xác nhận đơn hàng",
                    Body = $@"
Kính gửi Quý khách,

Cảm ơn bạn đã mua sắm tại cửa hàng của chúng tôi! Dưới đây là chi tiết hóa đơn của bạn:

Mã hóa đơn: {invoice.InvoiceID}
Tên người nhận: {invoice.ReceiverName}
Số điện thoại: {invoice.ReceiverPhone}
Địa chỉ giao hàng: {invoice.ReceiverAddress}
Danh sách sản phẩm:
{string.Join("\n", productList)}
Tổng sản phẩm: {invoice.TotalQuantity}
Tổng thanh toán: {invoice.TotalPayment:F0} VND
Phương thức thanh toán: {paymentType ?? invoice.PaymentType}
Phương thức giao hàng: {shippingMethod ?? invoice.ShippingMethod}
Trạng thái giao hàng: {shippingStatus ?? invoice.ShippingStatus}
Ngày tạo: {invoice.CreatedAt.ToString("g")}

Nếu có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.

Trân trọng,
Đội ngũ cửa hàng",
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

        public async Task<object> GetAccountInvoiceInfoAsync(string accountId)
        {
            try
            {
                if (string.IsNullOrEmpty(accountId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var accountExists = await _context.Accounts.AnyAsync(a => a.AccountID == accountId);
                if (!accountExists)
                    return new { errCode = 2, errMessage = "Tài khoản không tồn tại!", data = (object?)null };

                var data = await _context.Invoices
                    .Where(i => i.AccountID == accountId)
                    .Select(i => new
                    {
                        i.InvoiceID,
                        i.ReceiverName,
                        i.ReceiverPhone,
                        i.ReceiverAddress,
                        i.TotalQuantity,
                        i.TotalPayment,
                        i.CreatedAt,
                        i.CanceledAt,
                        i.PaymentStatus,
                        i.ShippingStatus
                    })
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                if (!data.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy đơn hàng nào!", data = new List<object>() };

                return new { errCode = 0, errMessage = "Lấy thông tin đơn hàng thành công!", data };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy thông tin đơn hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> LoadInvoiceInfoAsync(int page, int limit, string search, string filter, string sort, string date)
        {
            try
            {
                if (page < 1 || limit < 1)
                    return new { errCode = -1, errMessage = "Tham số page hoặc limit không hợp lệ!", data = (object?)null };

                if (filter != "ALL" && !filter.Contains("-"))
                    return new { errCode = 1, errMessage = "Tham số filter không hợp lệ!", data = (object?)null };

                if (!string.IsNullOrEmpty(sort) && !new[] { "0", "1", "2", "3", "4", "5", "6" }.Contains(sort))
                    return new { errCode = 1, errMessage = "Tham số sort không hợp lệ!", data = (object?)null };

                var query = _context.Invoices.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.Trim().Substring(0, Math.Min(search.Length, 50));
                    query = query.Where(i => i.ReceiverName.Contains(searchTerm) || i.ReceiverPhone.Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(date))
                {
                    if (!DateTime.TryParse(date, out var startOfDay))
                        return new { errCode = 1, errMessage = "Tham số date không hợp lệ!", data = (object?)null };

                    startOfDay = startOfDay.Date;
                    var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                    query = query.Where(i => i.CreatedAt >= startOfDay && i.CreatedAt <= endOfDay);
                }

                if (filter != "ALL")
                {
                    var parts = filter.Split('-');
                    var field = parts[0];
                    var value = parts[1];

                    if (field == "paymentstatus")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("PaymentStatus", value))
                            return new { errCode = 1, errMessage = "Trạng thái thanh toán không hợp lệ!", data = (object?)null };
                        query = query.Where(i => i.PaymentStatus == value);
                    }
                    else if (field == "shippingstatus")
                    {
                        if (!await _utilitiesRepository.CheckValidAllCodeAsync("ShippingStatus", value))
                            return new { errCode = 1, errMessage = "Trạng thái giao hàng không hợp lệ!", data = (object?)null };
                        query = query.Where(i => i.ShippingStatus == value);
                    }
                    else if (field == "totalpayment")
                    {
                        switch (value)
                        {
                            case "0":
                                query = query.Where(i => i.TotalPayment >= 500000 && i.TotalPayment <= 1000000);
                                break;
                            case "1":
                                query = query.Where(i => i.TotalPayment > 1000000 && i.TotalPayment <= 1500000);
                                break;
                            case "2":
                                query = query.Where(i => i.TotalPayment > 1500000 && i.TotalPayment <= 2000000);
                                break;
                            case "3":
                                query = query.Where(i => i.TotalPayment > 2000000);
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
                    "1" => query.OrderByDescending(i => i.CreatedAt),
                    "2" => query.OrderBy(i => i.CreatedAt),
                    "3" => query.OrderBy(i => i.TotalPayment),
                    "4" => query.OrderByDescending(i => i.TotalPayment),
                    "5" => query.OrderBy(i => i.TotalQuantity),
                    "6" => query.OrderByDescending(i => i.TotalQuantity),
                    _ => query.OrderByDescending(i => i.CreatedAt)
                };

                var totalItems = await query.CountAsync();
                var rows = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(i => new
                    {
                        i.InvoiceID,
                        i.ReceiverName,
                        i.ReceiverPhone,
                        i.TotalQuantity,
                        i.TotalPayment,
                        i.CreatedAt,
                        i.CanceledAt,
                        i.PaymentStatus,
                        i.ShippingStatus
                    })
                    .ToListAsync();

                if (!rows.Any())
                    return new { errCode = 0, errMessage = "Không tìm thấy đơn hàng nào!", data = new List<object>(), totalItems = 0 };

                return new { errCode = 0, errMessage = "Lấy danh sách đơn hàng thành công!", data = rows, totalItems };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> GetInvoiceDetailInfoAsync(string invoiceId)
        {
            try
            {
                if (string.IsNullOrEmpty(invoiceId))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var invoice = await _context.Invoices
                    .Where(i => i.InvoiceID == invoiceId)
                    .Select(i => new
                    {
                        i.InvoiceID,
                        i.TotalQuantity,
                        i.ReceiverName,
                        i.ReceiverPhone,
                        i.ReceiverAddress,
                        i.TotalPrice,
                        i.DiscountAmount,
                        i.TotalPayment,
                        i.CreatedAt,
                        i.PaymentType,
                        i.ShippingStatus,
                        i.ShippingMethod,
                        i.PaymentStatus,
                        i.CancelReason
                    })
                    .FirstOrDefaultAsync();

                if (invoice == null)
                    return new { errCode = 2, errMessage = "Hóa đơn không tồn tại!", data = (object?)null };

                var invoiceDetails = await _context.InvoiceDetails
                    .Where(id => id.InvoiceID == invoiceId)
                    .Select(id => new
                    {
                        id.ProductID,
                        id.ProductDetailID,
                        id.ItemQuantity,
                        id.ItemPrice
                    })
                    .ToListAsync();

                if (!invoiceDetails.Any())
                    return new { errCode = 2, errMessage = "Chi tiết đơn hàng không tồn tại!", data = (object?)null };

                var productList = new List<object>();
                foreach (var detail in invoiceDetails)
                {
                    var product = await _context.Products
                        .Where(p => p.ProductID == detail.ProductID)
                        .Select(p => new { p.ProductName, p.ProductPrice, p.ProductImage })
                        .FirstOrDefaultAsync();

                    var productDetail = await _context.ProductDetails
                        .Where(pd => pd.ProductDetailID == detail.ProductDetailID)
                        .Select(pd => new { pd.DetailName, pd.ExtraPrice, pd.Promotion })
                        .FirstOrDefaultAsync();

                    if (product == null || productDetail == null)
                        return new { errCode = 2, errMessage = $"Dữ liệu sản phẩm {detail.ProductID} hoặc chi tiết {detail.ProductDetailID} không tồn tại!", data = (object?)null };

                    productList.Add(new
                    {
                        ProductName = product.ProductName,
                        DetailName = productDetail.DetailName,
                        ProductImage = product.ProductImage,
                        ItemPrice = detail.ItemPrice,
                        ItemQuantity = detail.ItemQuantity
                    });
                }

                var data = new
                {
                    invoice.InvoiceID,
                    invoice.ReceiverName,
                    invoice.ReceiverPhone,
                    invoice.ReceiverAddress,
                    invoice.TotalQuantity,
                    invoice.TotalPrice,
                    invoice.DiscountAmount,
                    invoice.TotalPayment,
                    invoice.CreatedAt,
                    invoice.PaymentType,
                    invoice.ShippingMethod,
                    invoice.ShippingStatus,
                    invoice.PaymentStatus,
                    invoice.CancelReason,
                    ProductList = productList
                };

                return new { errCode = 0, errMessage = "Lấy chi tiết đơn hàng thành công!", data };
            }
            catch (Exception ex)
            {
                return new { errCode = 3, errMessage = $"Lỗi khi lấy chi tiết đơn hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> CreateInvoiceAsync(InvoiceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var validationResult = await ValidateInvoiceInput(request);
                if (validationResult != null)
                    return validationResult;

                var accountId = request.AccountID;
                if (string.IsNullOrEmpty(accountId))
                {
                    var guestIdResult = await _utilitiesRepository.GenerateIDAsync("G", 9, "Accounts", "AccountID");
                    if (guestIdResult == null)
                        return new { errCode = 3, errMessage = "Lỗi khi tạo ID khách hàng!", data = (object?)null };
                    accountId = guestIdResult;
                }

                var invoiceId = await _utilitiesRepository.GenerateIDAsync("DH", 8, "Invoices", "InvoiceID");
                if (invoiceId == null)
                    return new { errCode = 3, errMessage = "Lỗi khi tạo ID hóa đơn!", data = (object?)null };

                var invoice = new Invoice
                {
                    InvoiceID = invoiceId,
                    AccountID = accountId,
                    ReceiverName = request.ReceiverName.Trim(),
                    ReceiverPhone = request.ReceiverPhone.Trim(),
                    ReceiverAddress = request.ReceiverAddress.Trim(),
                    TotalQuantity = request.TotalQuantity,
                    TotalPrice = request.TotalPrice,
                    DiscountAmount = request.DiscountAmount,
                    TotalPayment = request.TotalPayment,
                    CreatedAt = DateTime.Now,
                    CanceledAt = null,
                    CancelReason = null,
                    PaymentStatus = request.PaymentStatus,
                    ShippingStatus = request.ShippingStatus,
                    PaymentType = request.PaymentType,
                    ShippingMethod = request.ShippingMethod,
                    CouponID = request.CouponID
                };

                _context.Invoices.Add(invoice);

                foreach (var item in request.CartItems)
                {
                    _context.InvoiceDetails.Add(new InvoiceDetail
                    {
                        InvoiceID = invoiceId,
                        ProductID = item.ProductID,
                        ProductDetailID = item.ProductDetailID,
                        ItemPrice = item.ItemPrice,
                        ItemQuantity = item.ItemQuantity
                    });

                    await _context.ProductDetails
                        .Where(pd => pd.ProductDetailID == item.ProductDetailID)
                        .ExecuteUpdateAsync(s => s.SetProperty(pd => pd.Stock, pd => pd.Stock - item.ItemQuantity));
                }

                if (!string.IsNullOrEmpty(request.AccountID) && !request.IsBuyNow)
                {
                    await _context.CartItems
                        .Where(ci => ci.AccountID == request.AccountID)
                        .ExecuteDeleteAsync();
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var emailSent = true;
                //if (!string.IsNullOrEmpty(request.Email))
                //    emailSent = await SendInvoiceEmailAsync(invoiceId, request.Email);

                if (!emailSent && !string.IsNullOrEmpty(request.Email))
                    return new { errCode = 0, errMessage = "Tạo đơn hàng thành công, nhưng gửi email thất bại!", data = new { InvoiceID = invoiceId } };

                return new { errCode = 0, errMessage = "Tạo đơn hàng thành công!", data = new { InvoiceID = invoiceId } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi tạo đơn hàng: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> ChangeInvoiceStatusAsync(ChangeInvoiceStatusRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceID) || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Status))
                    return new { errCode = -1, errMessage = "Thiếu tham số!", data = (object?)null };

                var invoice = await _context.Invoices
                    .Where(i => i.InvoiceID == request.InvoiceID)
                    .FirstOrDefaultAsync();

                if (invoice == null)
                    return new { errCode = 2, errMessage = "Hóa đơn không tồn tại!", data = (object?)null };

                if (!new[] { "PaymentStatus", "ShippingStatus" }.Contains(request.Type))
                    return new { errCode = 1, errMessage = "Loại trạng thái không hợp lệ!", data = (object?)null };

                var currentStatus = request.Type == "PaymentStatus" ? invoice.PaymentStatus : invoice.ShippingStatus;
                if (currentStatus == request.Status)
                    return new { errCode = 1, errMessage = "Trạng thái không thay đổi!", data = (object?)null };

                var validStatus = await _utilitiesRepository.CheckValidAllCodeAsync(request.Type == "PaymentStatus" ? "PaymentStatus" : "ShippingStatus", request.Status);
                if (!validStatus)
                    return new { errCode = 1, errMessage = $"Trạng thái {(request.Type == "PaymentStatus" ? "thanh toán" : "giao hàng")} không hợp lệ!", data = (object?)null };

                if (request.Type == "ShippingStatus" && new[] { "PEND_CANCEL", "CANCELED" }.Contains(request.Status))
                {
                    if (request.Status == "PEND_CANCEL" && string.IsNullOrEmpty(request.CancelReason))
                        return new { errCode = 1, errMessage = "Thiếu lý do hủy khi chuyển sang trạng thái chờ hủy!", data = (object?)null };

                    if (request.Status == "CANCELED" && string.IsNullOrEmpty(request.CancelReason) && string.IsNullOrEmpty(invoice.CancelReason))
                        return new { errCode = 1, errMessage = "Thiếu lý do hủy khi chuyển sang trạng thái đã hủy!", data = (object?)null };
                }

                if (request.Type == "PaymentStatus")
                {
                    invoice.PaymentStatus = request.Status;
                }
                else
                {
                    invoice.ShippingStatus = request.Status;
                    if (new[] { "PEND_CANCEL", "CANCELED" }.Contains(request.Status))
                        invoice.CancelReason = request.CancelReason?.Trim() ?? invoice.CancelReason;

                    if (request.Status == "CANCELED")
                    {
                        invoice.CanceledAt = DateTime.Now;
                        var invoiceDetails = await _context.InvoiceDetails
                            .Where(id => id.InvoiceID == request.InvoiceID)
                            .Select(id => new { id.ProductDetailID, id.ItemQuantity })
                            .ToListAsync();

                        foreach (var detail in invoiceDetails)
                        {
                            await _context.ProductDetails
                                .Where(pd => pd.ProductDetailID == detail.ProductDetailID)
                                .ExecuteUpdateAsync(s => s.SetProperty(pd => pd.Stock, pd => pd.Stock + detail.ItemQuantity));
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new { errCode = 0, errMessage = "Thay đổi trạng thái hóa đơn thành công!", data = (object?)null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { errCode = 3, errMessage = $"Lỗi khi thay đổi trạng thái: {ex.Message}", data = (object?)null };
            }
        }

        public async Task<object> SendInvoiceEmailAsyncs(string billId, string email)
        {
            try
            {
                var emailSent = await SendInvoiceEmailAsync(billId, email);
                if (emailSent)
                    return new { errCode = 0, errMessage = "Gửi email hóa đơn thành công!", data = (object?)null };
                else
                    return new { errCode = 1, errMessage = "Gửi email hóa đơn thất bại!", data = (object?)null };
            }
            catch (Exception ex)
            {
                return new { errCode = 2, errMessage = $"Lỗi khi gửi email hóa đơn: {ex.Message}", data = (object?)null };
            }
        }
    }
}