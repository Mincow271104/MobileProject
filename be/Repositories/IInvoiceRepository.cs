using be.Models.DTOs;
using System.Threading.Tasks;
namespace be.Repositories
{


    public interface IInvoiceRepository
    {
        Task<object> GetAccountInvoiceInfoAsync(string accountId);
        Task<object> LoadInvoiceInfoAsync(int page, int limit, string search, string filter, string sort, string date);
        Task<object> GetInvoiceDetailInfoAsync(string invoiceId);
        Task<object> CreateInvoiceAsync(InvoiceRequest request);
        Task<object> ChangeInvoiceStatusAsync(ChangeInvoiceStatusRequest request);
        Task<object> SendInvoiceEmailAsyncs(string billId, string email);
    }
}