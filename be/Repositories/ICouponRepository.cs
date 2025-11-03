using be.Models.DTOs;
using System.Threading.Tasks;

namespace be.Repositories
{

    public interface ICouponRepository
    {
        Task<object> GetCouponInfoAsync(string couponCode);
        Task<object> LoadCouponInfoAsync(int page, int limit, string search, string filter, string sort, string date);
        Task<object> CreateCouponAsync(CouponRequest request);
        Task<object> ChangeCouponInfoAsync(CouponRequest request);
        Task<object> CheckCouponAsync(string couponCode, decimal price);
    }
}