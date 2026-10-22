using System.Text.Json;
using CinemaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    public class BookingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BookingController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5027";
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị giao diện sơ đồ ghế cho suất chiếu.
        /// GET: /Booking/SeatMap/{id?}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SeatMap(int? id)
        {
            int showtimeId = id ?? 1;
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync($"{_apiBaseUrl}/api/showtimes/{showtimeId}/seat-map");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var model = JsonSerializer.Deserialize<SeatMapViewModel>(content, options);

                    if (model != null)
                    {
                        return View(model);
                    }
                }
                else
                {
                    _logger.LogWarning("Không thể tải sơ đồ ghế từ API. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối tới Web API ({BaseUrl}) khi tải sơ đồ ghế.", _apiBaseUrl);
            }

            // Fallback UI mẫu nếu chưa chạy API để đảm bảo khung giao diện luôn xem được
            var fallbackModel = CreateFallbackSeatMap(showtimeId);
            fallbackModel.ErrorMessage = "Đang hiển thị chế độ xem mẫu (Vui lòng đảm bảo Web API cổng 5027 đang chạy để cập nhật dữ liệu trực tiếp).";
            return View(fallbackModel);
        }

        private static SeatMapViewModel CreateFallbackSeatMap(int showtimeId)
        {
            var seats = new List<SeatItemViewModel>();
            int seatIdCounter = 1;

            // Hàng A (1-10): Ghế Thường
            for (int i = 1; i <= 10; i++)
            {
                seats.Add(new SeatItemViewModel
                {
                    SeatId = seatIdCounter++,
                    RowCode = "A",
                    SeatNumber = i,
                    SeatTypeId = 1,
                    SeatTypeName = "Standard",
                    ExtraPrice = 0,
                    FinalPrice = 80000,
                    Status = "Available"
                });
            }

            // Hàng B (1-10): Ghế VIP
            for (int i = 1; i <= 10; i++)
            {
                string status = "Available";
                if (i == 5 || i == 6) status = "Sold";
                else if (i == 7) status = "Held";

                seats.Add(new SeatItemViewModel
                {
                    SeatId = seatIdCounter++,
                    RowCode = "B",
                    SeatNumber = i,
                    SeatTypeId = 2,
                    SeatTypeName = "VIP",
                    ExtraPrice = 20000,
                    FinalPrice = 100000,
                    Status = status,
                    HeldSecondsRemaining = status == "Held" ? 280 : null
                });
            }

            // Hàng C (1-10): Ghế Couple, ghế C10 bảo trì
            for (int i = 1; i <= 10; i++)
            {
                string status = i == 10 ? "Disabled" : "Available";
                seats.Add(new SeatItemViewModel
                {
                    SeatId = seatIdCounter++,
                    RowCode = "C",
                    SeatNumber = i,
                    SeatTypeId = 3,
                    SeatTypeName = "Couple",
                    ExtraPrice = 40000,
                    FinalPrice = 120000,
                    Status = status
                });
            }

            return new SeatMapViewModel
            {
                ShowtimeId = showtimeId,
                MovieId = 1,
                MovieTitle = "Deadpool & Wolverine",
                PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80",
                RoomId = 1,
                RoomName = "Phòng Chiếu 01",
                ScreenType = "2D Digital",
                StartTime = DateTime.Today.AddHours(19),
                EndTime = DateTime.Today.AddHours(21).AddMinutes(15),
                BasePrice = 80000,
                TotalSeats = seats.Count,
                AvailableCount = seats.Count(s => s.Status == "Available"),
                HeldCount = seats.Count(s => s.Status == "Held"),
                SoldCount = seats.Count(s => s.Status == "Sold"),
                DisabledCount = seats.Count(s => s.Status == "Disabled"),
                Seats = seats
            };
        }
    }
}
