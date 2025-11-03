namespace be.Controllers
{
    using be.Models.DTOs;
    using be.Repositories;
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    [ApiController]
    public class BannerApiController : ControllerBase
    {
        private readonly IBannerRepository _bannerRepository;

        public BannerApiController(IBannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        [HttpGet("get-sale-banner-info")]
        public async Task<IActionResult> GetSaleBannerInfo([FromQuery] string productid)
        {
            try
            {
                var response = await _bannerRepository.GetBannerSaleInfoAsync(productid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("get-banner-info")]
        public async Task<IActionResult> GetBannerInfo([FromQuery] string bannerid)
        {
            try
            {
                var response = await _bannerRepository.GetBannerInfoAsync(bannerid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpGet("load-banner-info")]
        public async Task<IActionResult> LoadBannerInfo([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "", [FromQuery] string filter = "ALL", [FromQuery] string sort = "0", [FromQuery] string date = "")
        {
            try
            {
                var response = await _bannerRepository.LoadBannerInfoAsync(page, limit, search, filter, sort, date);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("create-banner")]
        public async Task<IActionResult> CreateBanner([FromBody] BannerRequest request)
        {
            try
            {
                var response = await _bannerRepository.CreateBannerAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }

        [HttpPost("change-banner-info")]
        public async Task<IActionResult> ChangeBannerInfo([FromBody] BannerRequest request)
        {
            try
            {
                var response = await _bannerRepository.ChangeBannerInfoAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}