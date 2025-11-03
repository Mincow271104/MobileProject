using be.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Ghi chú: Controller cho endpoint get-allcodes, ánh xạ từ handleGetAllCodes trong utilitiesController.js
namespace be.Controllers
{
    [Route("api")]
    [ApiController]
    public class UtilitiesApiController : ControllerBase
    {
        private readonly IUtilitiesRepository _utilitiesRepository;

        public UtilitiesApiController(IUtilitiesRepository utilitiesRepository)
        {
            _utilitiesRepository = utilitiesRepository;
        }

        [HttpGet("get-allcodes")]
        public async Task<IActionResult> GetAllCodes([FromQuery] string type)
        {
            try
            {
                var response = await _utilitiesRepository.GetAllCodesAsync(type);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errCode = 3, errMessage = $"Lỗi từ server: {ex.Message}", data = (object?)null });
            }
        }
    }
}
