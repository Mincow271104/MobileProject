using be.Models.DTOs;
using be.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace be.Controllers
{


    [Route("api")]
    [ApiController]
    public class CouponApiController : ControllerBase
    {
        private readonly ICouponRepository _couponRepository;

        public CouponApiController(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        [HttpGet("get-coupon-info")]
        public async Task<IActionResult> GetCouponInfo([FromQuery] string couponcode)
        {
            try
            {
                var response = await _couponRepository.GetCouponInfoAsync(couponcode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-coupon-info")]
        public async Task<IActionResult> LoadCouponInfo([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0", [FromQuery] string date = "")
        {
            try
            {
                var response = await _couponRepository.LoadCouponInfoAsync(page, limit, search, filter, sort, date);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("create-coupon")]
        public async Task<IActionResult> CreateCoupon([FromBody] CouponRequest request)
        {
            try
            {
                var response = await _couponRepository.CreateCouponAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("check-coupon")]
        public async Task<IActionResult> CheckCoupon([FromQuery] string couponcode, [FromQuery] decimal price)
        {
            try
            {
                var response = await _couponRepository.CheckCouponAsync(couponcode, price);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-coupon-info")]
        public async Task<IActionResult> ChangeCouponInfo([FromBody] CouponRequest request)
        {
            try
            {
                var response = await _couponRepository.ChangeCouponInfoAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}