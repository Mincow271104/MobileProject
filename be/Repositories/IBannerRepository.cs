using be.Models.DTOs;
using System.Threading.Tasks;
namespace be.Repositories
{


    public interface IBannerRepository
    {
        Task<object> GetBannerSaleInfoAsync(string productId);
        Task<object> GetBannerInfoAsync(string bannerId);
        Task<object> LoadBannerInfoAsync(int page, int limit, string search, string filter, string sort, string date);
        Task<object> CreateBannerAsync(BannerRequest request);
        Task<object> ChangeBannerInfoAsync(BannerRequest request);
    }
}