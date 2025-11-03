using be.Models.DTOs;
using System.Threading.Tasks;
namespace be.Repositories
{


    public interface ICartRepository
    {
        Task<object> GetCartAsync(string accountId);
        Task<object> GetCartDetailAsync(CartInfoRequest[] cartInfo);
        Task<object> GetDetailListAsync(CartInfoRequest[] cartInfo);
        Task<object> AddToCartAsync(AddToCartRequest request);
        Task<object> UpdateQuantityAsync(UpdateQuantityRequest request);
        Task<object> UpdateCartDetailAsync(UpdateCartDetailRequest request);
        Task<object> MergeCartDetailAsync(MergeCartDetailRequest request);
        Task<object> RemoveFromCartAsync(RemoveFromCartRequest request);
    }
}