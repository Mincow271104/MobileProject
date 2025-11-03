using Microsoft.AspNetCore.Mvc;

namespace be.Controllers
{
    using be.Models.DTOs;
    using be.Repositories;
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductApiController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet("get-sale-product-info")]
        public async Task<IActionResult> GetSaleProductInfo([FromQuery] string productid)
        {
            try
            {
                var response = await _productRepository.GetSaleProductInfoAsync(productid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-sale-product-info")]
        public async Task<IActionResult> LoadSaleProductInfo([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0")
        {
            try
            {
                var response = await _productRepository.LoadSaleProductInfoAsync(page, limit, search, filter, sort);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-product-info")]
        public async Task<IActionResult> GetProductInfo([FromQuery] string productid)
        {
            try
            {
                var response = await _productRepository.GetProductInfoAsync(productid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-product-info")]
        public async Task<IActionResult> LoadProductInfo([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0")
        {
            try
            {
                var response = await _productRepository.LoadProductInfoAsync(page, limit, search, filter, sort);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-product-detail-info")]
        public async Task<IActionResult> GetProductDetailInfo([FromQuery] string productid, [FromQuery] string productdetailid)
        {
            try
            {
                var response = await _productRepository.GetProductDetailInfoAsync(productid, productdetailid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var response = await _productRepository.CreateProductAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-product-info")]
        public async Task<IActionResult> ChangeProductInfo([FromBody] ChangeProductInfoRequest request)
        {
            try
            {
                var response = await _productRepository.ChangeProductInfoAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-filtered-product-info")]
        public async Task<IActionResult> LoadFilteredProductInfo([FromQuery] string filterProductType = "ALL", [FromQuery] string[] filterPetType = null)
        {
            try
            {
                var response = await _productRepository.LoadFilteredProductInfoAsync(filterProductType, filterPetType ?? new[] { "ALL" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}
