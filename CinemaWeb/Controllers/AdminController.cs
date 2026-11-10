using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    /// <summary>
    /// Controller điều hướng chính cho phân hệ Quản trị (Admin Console).
    /// Áp dụng bảo vệ [Authorize(Roles = "Admin,Staff")] theo yêu cầu Task 13.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AdminController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5027";
            _logger = logger;
        }

        // 1. Dashboard (Bảng điều khiển chung)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. Quản lý Phim (Dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Movies()
        {
            return View();
        }

        // 3. Quản lý Suất chiếu (Admin & Staff)
        [HttpGet]
        public IActionResult Showtimes()
        {
            return View();
        }

        // 4. Quản lý Combo bắp nước (Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Combos()
        {
            return View();
        }

        // 5. Quản lý Thể loại phim (Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Genres()
        {
            return View();
        }

        // 6. Quản lý Người dùng & Phân quyền (Admin duy nhất)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Users()
        {
            return View();
        }

        #region Reports API Proxies for Chart.js Dashboard

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetDailySummaryApi()
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync($"{_apiBaseUrl}/api/reports/daily-summary");
                var content = await response.Content.ReadAsStringAsync();
                Response.StatusCode = (int)response.StatusCode;
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API reports daily-summary");
                return StatusCode(500, new { message = "Lỗi kết nối máy chủ báo cáo." });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRevenueReportApi([FromQuery] string? from, [FromQuery] string? to)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var url = $"{_apiBaseUrl}/api/reports/revenue";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={from}");
                if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={to}");
                if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                Response.StatusCode = (int)response.StatusCode;
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API reports revenue");
                return StatusCode(500, new { message = "Lỗi kết nối máy chủ báo cáo." });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetTopMoviesApi([FromQuery] int limit = 5)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync($"{_apiBaseUrl}/api/reports/top-movies?limit={limit}");
                var content = await response.Content.ReadAsStringAsync();
                Response.StatusCode = (int)response.StatusCode;
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API reports top-movies");
                return StatusCode(500, new { message = "Lỗi kết nối máy chủ báo cáo." });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetOccupancyApi([FromQuery] string? from, [FromQuery] string? to)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var url = $"{_apiBaseUrl}/api/reports/occupancy";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={from}");
                if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={to}");
                if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                Response.StatusCode = (int)response.StatusCode;
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API reports occupancy");
                return StatusCode(500, new { message = "Lỗi kết nối máy chủ báo cáo." });
            }
        }

        #endregion
    }
}
