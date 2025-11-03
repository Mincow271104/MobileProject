using be.Models.DTOs;
using be.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace be.Controllers
{


    [Route("api")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;

        public CartApiController(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        [HttpGet("get-cart")]
        public async Task<IActionResult> GetCart([FromQuery] string accountid)
        {
            try
            {
                var response = await _cartRepository.GetCartAsync(accountid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-cart-detail")]
        public async Task<IActionResult> GetCartDetail([FromQuery] string cartInfo)
        {
            try
            {
                var cartInfoArray = JsonSerializer.Deserialize<CartInfoRequest[]>(cartInfo);
                var response = await _cartRepository.GetCartDetailAsync(cartInfoArray);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-detail-list")]
        public async Task<IActionResult> GetDetailList([FromQuery] string cartInfo)
        {
            try
            {
                var cartInfoArray = JsonSerializer.Deserialize<CartInfoRequest[]>(cartInfo);
                var response = await _cartRepository.GetDetailListAsync(cartInfoArray);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var response = await _cartRepository.AddToCartAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var response = await _cartRepository.UpdateQuantityAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("update-cart-detail")]
        public async Task<IActionResult> UpdateCartDetail([FromBody] UpdateCartDetailRequest request)
        {
            try
            {
                var response = await _cartRepository.UpdateCartDetailAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("merge-cart-detail")]
        public async Task<IActionResult> MergeCartDetail([FromBody] MergeCartDetailRequest request)
        {
            try
            {
                var response = await _cartRepository.MergeCartDetailAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("remove-from-cart")]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                var response = await _cartRepository.RemoveFromCartAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}