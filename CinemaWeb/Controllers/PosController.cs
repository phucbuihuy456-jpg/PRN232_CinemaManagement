using System.Text;
using System.Text.Json;
using CinemaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    public class PosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<PosController> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public PosController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PosController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5027";
            _logger = logger;
        }

        /// <summary>
        /// Task 31: Màn hình Staff POS Bán vé nhanh tại quầy.
        /// GET: /Pos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var model = new PosViewModel();

            // 1. Tải danh sách suất chiếu từ API
            try
            {
                var showtimesRes = await client.GetAsync($"{_apiBaseUrl}/api/showtimes");
                if (showtimesRes.IsSuccessStatusCode)
                {
                    var content = await showtimesRes.Content.ReadAsStringAsync();
                    var showtimes = JsonSerializer.Deserialize<List<PosShowtimeItem>>(content, JsonOptions);
                    if (showtimes != null && showtimes.Any())
                    {
                        model.Showtimes = showtimes.OrderBy(s => s.StartTime).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi kết nối khi tải suất chiếu cho POS.");
            }

            // Fallback suất chiếu nếu API chưa có hoặc lỗi
            if (!model.Showtimes.Any())
            {
                model.Showtimes = CreateFallbackShowtimes();
            }

            // 2. Tải danh sách Combo bắp nước từ API
            try
            {
                var combosRes = await client.GetAsync($"{_apiBaseUrl}/api/combos");
                if (combosRes.IsSuccessStatusCode)
                {
                    var content = await combosRes.Content.ReadAsStringAsync();
                    var combos = JsonSerializer.Deserialize<List<ComboItemViewModel>>(content, JsonOptions);
                    if (combos != null && combos.Any())
                    {
                        model.Combos = combos.Where(c => c.IsActive).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi kết nối khi tải danh sách combo cho POS.");
            }

            if (!model.Combos.Any())
            {
                model.Combos = CreateFallbackCombos();
            }

            return View(model);
        }

        /// <summary>
        /// API Proxy tải sơ đồ ghế cho suất chiếu tại giao diện POS.
        /// GET: /Pos/GetSeatMap/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSeatMap(int id)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var res = await client.GetAsync($"{_apiBaseUrl}/api/showtimes/{id}/seat-map");
                if (res.IsSuccessStatusCode)
                {
                    var content = await res.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return StatusCode((int)res.StatusCode, new { message = "Không thể tải sơ đồ ghế." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải sơ đồ ghế POS cho suất chiếu #{Id}", id);
                return StatusCode(500, new { message = "Lỗi kết nối máy chủ API." });
            }
        }

        /// <summary>
        /// Task 31: Xử lý bán vé nhanh tại quầy POS với tiền mặt (Cash) / Chuyển khoản.
        /// Quy trình: HoldSeats -> Checkout với Cash -> Trả về thông tin hóa đơn & mã QR in ngay.
        /// POST: /Pos/QuickCheckout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> QuickCheckout([FromBody] PosCheckoutRequestModel request)
        {
            if (request == null || request.SeatIds == null || !request.SeatIds.Any())
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất 1 ghế cho khách." });
            }

            var client = _httpClientFactory.CreateClient();

            try
            {
                // Bước 1: Giữ ghế tạm thời (HoldSeats)
                var holdPayload = new
                {
                    showtimeId = request.ShowtimeId,
                    seatIds = request.SeatIds,
                    userId = (int?)null
                };

                var holdContent = new StringContent(JsonSerializer.Serialize(holdPayload), Encoding.UTF8, "application/json");
                var holdRes = await client.PostAsync($"{_apiBaseUrl}/api/bookings/hold-seats", holdContent);
                var holdJsonStr = await holdRes.Content.ReadAsStringAsync();

                if (!holdRes.IsSuccessStatusCode)
                {
                    return StatusCode((int)holdRes.StatusCode, Content(holdJsonStr, "application/json"));
                }

                var holdData = JsonSerializer.Deserialize<JsonElement>(holdJsonStr);
                string holdToken = holdData.GetProperty("holdToken").GetString() ?? string.Empty;

                // Bước 2: Thực hiện Checkout ngay lập tức với tiền mặt (Cash) hoặc phương thức chọn
                var combosDto = request.Combos?.Where(c => c.Quantity > 0).Select(c => new
                {
                    comboId = c.ComboId,
                    quantity = c.Quantity
                }).ToList();

                var checkoutPayload = new
                {
                    showtimeId = request.ShowtimeId,
                    holdToken = holdToken,
                    seatIds = request.SeatIds,
                    combos = combosDto,
                    paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod,
                    customerName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Khách mua tại quầy" : request.CustomerName,
                    customerPhone = request.CustomerPhone,
                    customerEmail = request.CustomerEmail
                };

                var checkoutContent = new StringContent(JsonSerializer.Serialize(checkoutPayload), Encoding.UTF8, "application/json");
                var checkoutRes = await client.PostAsync($"{_apiBaseUrl}/api/bookings/checkout", checkoutContent);
                var checkoutJsonStr = await checkoutRes.Content.ReadAsStringAsync();

                if (!checkoutRes.IsSuccessStatusCode)
                {
                    return StatusCode((int)checkoutRes.StatusCode, Content(checkoutJsonStr, "application/json"));
                }

                // Bổ sung thông tin tính tiền thừa (Change) cho Staff
                using var doc = JsonDocument.Parse(checkoutJsonStr);
                var root = doc.RootElement;
                decimal totalAmount = root.GetProperty("totalAmount").GetDecimal();
                decimal cashReceived = request.CashReceived >= totalAmount ? request.CashReceived : totalAmount;
                decimal changeAmount = cashReceived - totalAmount;

                var resultObj = new
                {
                    success = true,
                    bookingId = root.GetProperty("bookingId").GetInt32(),
                    qrCodeToken = root.GetProperty("qrCodeToken").GetString(),
                    totalAmount = totalAmount,
                    cashReceived = cashReceived,
                    changeAmount = changeAmount,
                    paymentMethod = root.GetProperty("paymentMethod").GetString(),
                    createdAt = root.GetProperty("createdAt").GetString(),
                    showtime = root.GetProperty("showtime"),
                    seats = root.GetProperty("seats"),
                    combos = root.GetProperty("combos"),
                    customer = root.GetProperty("customer"),
                    message = "Thanh toán thành công! Vé và hóa đơn đã được tạo."
                };

                return Ok(resultObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình POS QuickCheckout.");
                return StatusCode(500, new { message = "Lỗi kết nối tới hệ thống máy chủ bán vé." });
            }
        }

        private static List<PosShowtimeItem> CreateFallbackShowtimes()
        {
            var today = DateTime.Today;
            return new List<PosShowtimeItem>
            {
                new()
                {
                    Id = 1,
                    MovieId = 1,
                    MovieTitle = "Deadpool & Wolverine",
                    PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80",
                    RoomId = 1,
                    RoomName = "Phòng Chiếu 01",
                    ScreenType = "2D Digital",
                    StartTime = today.AddHours(14),
                    EndTime = today.AddHours(16).AddMinutes(15),
                    BasePrice = 80000,
                    AvailableSeats = 27,
                    TotalSeats = 30
                },
                new()
                {
                    Id = 2,
                    MovieId = 2,
                    MovieTitle = "Inside Out 2",
                    PosterUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=500&q=80",
                    RoomId = 2,
                    RoomName = "Phòng Chiếu 02",
                    ScreenType = "3D IMAX",
                    StartTime = today.AddHours(18),
                    EndTime = today.AddHours(20).AddMinutes(00),
                    BasePrice = 120000,
                    AvailableSeats = 30,
                    TotalSeats = 30
                },
                new()
                {
                    Id = 3,
                    MovieId = 1,
                    MovieTitle = "Deadpool & Wolverine",
                    PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80",
                    RoomId = 1,
                    RoomName = "Phòng Chiếu 01",
                    ScreenType = "2D Digital",
                    StartTime = today.AddHours(20).AddMinutes(30),
                    EndTime = today.AddHours(22).AddMinutes(45),
                    BasePrice = 85000,
                    AvailableSeats = 28,
                    TotalSeats = 30
                }
            };
        }

        private static List<ComboItemViewModel> CreateFallbackCombos()
        {
            return new List<ComboItemViewModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Combo Solo",
                    Description = "1 Bắp ngọt vừa + 1 Nước ngọt",
                    Price = 85000,
                    ImageUrl = "https://images.unsplash.com/photo-1585647347384-2593bc35786b?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                },
                new()
                {
                    Id = 2,
                    Name = "Combo Couple",
                    Description = "1 Bắp lớn + 2 Nước ngọt",
                    Price = 119000,
                    ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                },
                new()
                {
                    Id = 3,
                    Name = "Combo Party Family",
                    Description = "2 Bắp lớn + 4 Nước + 1 Snack",
                    Price = 209000,
                    ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                }
            };
        }
    }
}
