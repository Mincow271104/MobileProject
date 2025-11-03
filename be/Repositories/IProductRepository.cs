using be.Models.DTOs;
using System.Threading.Tasks;
namespace be.Repositories
{


    public interface IProductRepository
    {
        Task<object> GetSaleProductInfoAsync(string productId);
        Task<object> LoadSaleProductInfoAsync(int page, int limit, string search, string filter, string sort);
        Task<object> GetProductInfoAsync(string productId);
        Task<object> LoadProductInfoAsync(int page, int limit, string search, string filter, string sort);
        Task<object> GetProductDetailInfoAsync(string productId, string productDetailId);
        Task<object> CreateProductAsync(CreateProductRequest request);
        Task<object> ChangeProductInfoAsync(ChangeProductInfoRequest request);
        Task<object> LoadFilteredProductInfoAsync(string filterProductType, string[] filterPetType);
    }
}