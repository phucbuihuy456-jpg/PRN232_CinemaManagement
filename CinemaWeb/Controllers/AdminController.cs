using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CinemaWeb.Models;
using CinemaWeb.Services;
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
        private readonly IMovieApiClient _movieApiClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AdminController> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AdminController(
            IHttpClientFactory httpClientFactory,
            IMovieApiClient movieApiClient,
            IConfiguration configuration,
            ILogger<AdminController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _movieApiClient = movieApiClient;
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
        public async Task<IActionResult> Showtimes([FromQuery] int? movieId, [FromQuery] string? date)
        {
            var model = new AdminShowtimesListViewModel
            {
                SelectedMovieId = movieId,
                SelectedDate = date
            };

            var client = _httpClientFactory.CreateClient();

            // 1. Tải danh sách phim để lọc hoặc tạo mới suất chiếu
            try
            {
                model.Movies = await _movieApiClient.GetMoviesFromODataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi tải danh sách phim cho Admin Showtimes.");
            }

            // 2. Tải danh sách phòng chiếu từ API
            try
            {
                var roomsRes = await client.GetAsync($"{_apiBaseUrl}/api/rooms");
                if (roomsRes.IsSuccessStatusCode)
                {
                    var roomsContent = await roomsRes.Content.ReadAsStringAsync();
                    model.Rooms = JsonSerializer.Deserialize<List<AdminRoomItemViewModel>>(roomsContent, JsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi tải danh sách phòng chiếu.");
            }

            // Fallback danh sách phòng mẫu nếu API chưa trả về
            if (!model.Rooms.Any())
            {
                model.Rooms = new List<AdminRoomItemViewModel>
                {
                    new() { Id = 1, Name = "Phòng Chiếu 01", ScreenType = "2D Digital", TotalSeats = 30 },
                    new() { Id = 2, Name = "Phòng Chiếu 02", ScreenType = "3D IMAX", TotalSeats = 30 }
                };
            }

            // 3. Tải danh sách suất chiếu từ API (có hỗ trợ lọc theo movieId hoặc date)
            try
            {
                var queryParams = new List<string>();
                if (movieId.HasValue && movieId.Value > 0)
                {
                    queryParams.Add($"movieId={movieId.Value}");
                }
                if (!string.IsNullOrWhiteSpace(date))
                {
                    queryParams.Add($"date={date}");
                }

                var queryStr = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var res = await client.GetAsync($"{_apiBaseUrl}/api/showtimes{queryStr}");

                if (res.IsSuccessStatusCode)
                {
                    var content = await res.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<AdminShowtimeItemViewModel>>(content, JsonOptions);
                    if (list != null)
                    {
                        model.Showtimes = list.OrderByDescending(s => s.StartTime).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API showtimes cho Admin.");
            }

            return View(model);
        }

        /// <summary>
        /// Tạo suất chiếu mới (Chỉ dành cho Admin).
        /// POST: /Admin/CreateShowtime
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateShowtime([FromBody] CreateShowtimeRequestModel request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Dữ liệu tạo suất chiếu không hợp lệ." });
            }

            var client = _httpClientFactory.CreateClient();

            // Đính kèm JWT Token của Admin từ Cookie Claims
            var token = User.FindFirst("JwtToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_apiBaseUrl}/api/showtimes", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Response.StatusCode = (int)response.StatusCode;
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API tạo suất chiếu mới.");
                return StatusCode(500, new { message = "Không thể kết nối đến máy chủ quản lý suất chiếu." });
            }
        }

        /// <summary>
        /// Xóa mềm một suất chiếu (Chỉ dành cho Admin).
        /// POST / DELETE: /Admin/DeleteShowtime/{id}
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [HttpDelete]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Đính kèm JWT Token của Admin từ Cookie Claims
            var token = User.FindFirst("JwtToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/showtimes/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                Response.StatusCode = (int)response.StatusCode;
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API xóa suất chiếu ID {Id}", id);
                return StatusCode(500, new { message = "Không thể kết nối đến máy chủ API để xóa suất chiếu." });
            }
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
