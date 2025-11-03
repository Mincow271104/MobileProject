using be.Models.DTOs;
using be.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace be.Controllers
{

    [Route("api")]
    [ApiController]
    public class InvoiceApiController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceApiController(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        [HttpGet("get-account-invoice-info")]
        public async Task<IActionResult> GetAccountInvoiceInfo([FromQuery] string accountid)
        {
            try
            {
                var response = await _invoiceRepository.GetAccountInvoiceInfoAsync(accountid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-invoice-info")]
        public async Task<IActionResult> LoadInvoiceInfo([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0", [FromQuery] string date = "")
        {
            try
            {
                var response = await _invoiceRepository.LoadInvoiceInfoAsync(page, limit, search, filter, sort, date);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-invoice-detail-info")]
        public async Task<IActionResult> GetInvoiceDetailInfo([FromQuery] string invoiceid)
        {
            try
            {
                var response = await _invoiceRepository.GetInvoiceDetailInfoAsync(invoiceid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceRequest request)
        {
            try
            {
                var response = await _invoiceRepository.CreateInvoiceAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-invoice-status")]
        public async Task<IActionResult> ChangeInvoiceStatus([FromBody] ChangeInvoiceStatusRequest request)
        {
            try
            {
                var response = await _invoiceRepository.ChangeInvoiceStatusAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("get-invoice-email")]
        public async Task<IActionResult> GetInvoiceEmail([FromBody] SendInvoiceEmailRequest request)
        {
            try
            {
                var response = await _invoiceRepository.SendInvoiceEmailAsyncs(request.BillID, request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}