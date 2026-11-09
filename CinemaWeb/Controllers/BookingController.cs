using System.Text;
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
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
                    var model = JsonSerializer.Deserialize<SeatMapViewModel>(content, JsonOptions);

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

            // Fallback UI mẫu nếu chưa chạy API
            var fallbackModel = CreateFallbackSeatMap(showtimeId);
            fallbackModel.ErrorMessage = "Đang hiển thị chế độ xem mẫu (Vui lòng đảm bảo Web API cổng 5027 đang chạy để cập nhật dữ liệu trực tiếp).";
            return View(fallbackModel);
        }

        /// <summary>
        /// API Proxy cho AJAX client lấy trạng thái ghế thời gian thực mỗi 10 giây (Task 20).
        /// GET: /Booking/GetSeatMapStatus/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSeatMapStatus(int id)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync($"{_apiBaseUrl}/api/showtimes/{id}/seat-map");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return StatusCode((int)response.StatusCode, new { message = "Lỗi khi lấy trạng thái ghế từ API." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kết nối API seat-map");
                return StatusCode(500, new { message = "Không thể kết nối đến máy chủ API." });
            }
        }

        /// <summary>
        /// API Proxy cho AJAX client gửi yêu cầu giữ ghế tạm 5 phút (Task 19).
        /// POST: /Booking/HoldSeats
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> HoldSeats([FromBody] JsonElement payload)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_apiBaseUrl}/api/bookings/hold-seats", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, Content(responseContent, "application/json"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API hold-seats");
                return StatusCode(500, new { message = "Không thể kết nối đến máy chủ giữ ghế." });
            }
        }

        /// <summary>
        /// Task 21: Giao diện Chọn Combo & Tóm tắt đơn vé.
        /// GET: /Booking/Checkout?showtimeId=1&holdToken=xyz&seatIds=1,2,3
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout(int showtimeId, string holdToken, string? seatIds)
        {
            if (string.IsNullOrWhiteSpace(holdToken))
            {
                return RedirectToAction("SeatMap", new { id = showtimeId });
            }

            var client = _httpClientFactory.CreateClient();
            var model = new CheckoutViewModel
            {
                ShowtimeId = showtimeId,
                HoldToken = holdToken,
                ExpiresAt = DateTime.Now.AddMinutes(5),
                RemainingSeconds = 300
            };

            // 1. Lấy thông tin sơ đồ ghế và suất chiếu
            try
            {
                var seatMapResponse = await client.GetAsync($"{_apiBaseUrl}/api/showtimes/{showtimeId}/seat-map");
                if (seatMapResponse.IsSuccessStatusCode)
                {
                    var content = await seatMapResponse.Content.ReadAsStringAsync();
                    var seatMap = JsonSerializer.Deserialize<SeatMapViewModel>(content, JsonOptions);

                    if (seatMap != null)
                    {
                        model.MovieId = seatMap.MovieId;
                        model.MovieTitle = seatMap.MovieTitle;
                        model.PosterUrl = seatMap.PosterUrl;
                        model.RoomId = seatMap.RoomId;
                        model.RoomName = seatMap.RoomName;
                        model.ScreenType = seatMap.ScreenType;
                        model.StartTime = seatMap.StartTime;
                        model.EndTime = seatMap.EndTime;
                        model.BasePrice = seatMap.BasePrice;

                        // Lọc các ghế khách đã chọn
                        var targetSeatIdList = (seatIds ?? string.Empty)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s, out int sid) ? sid : 0)
                            .Where(sid => sid > 0)
                            .ToHashSet();

                        if (targetSeatIdList.Any())
                        {
                            model.SelectedSeats = seatMap.Seats
                                .Where(s => targetSeatIdList.Contains(s.SeatId))
                                .ToList();
                        }
                        else
                        {
                            // Nếu không truyền query seatIds, lấy các ghế có Status = Held
                            model.SelectedSeats = seatMap.Seats
                                .Where(s => s.Status == "Held")
                                .ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải thông tin suất chiếu từ API.");
            }

            // 2. Lấy danh sách Combo bắp nước từ API (Task 7 & Task 21)
            try
            {
                var combosResponse = await client.GetAsync($"{_apiBaseUrl}/api/combos");
                if (combosResponse.IsSuccessStatusCode)
                {
                    var content = await combosResponse.Content.ReadAsStringAsync();
                    var combos = JsonSerializer.Deserialize<List<ComboItemViewModel>>(content, JsonOptions);
                    if (combos != null)
                    {
                        model.AvailableCombos = combos.Where(c => c.IsActive).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách combo từ API.");
            }

            // Fallback combo nếu chưa có data
            if (!model.AvailableCombos.Any())
            {
                model.AvailableCombos = CreateFallbackCombos();
            }

            return View(model);
        }

        /// <summary>
        /// Task 22: Tiếp nhận yêu cầu Checkout từ Form / AJAX và gửi tới API Checkout DB Transaction.
        /// POST: /Booking/ProcessCheckout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout([FromBody] JsonElement payload)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_apiBaseUrl}/api/bookings/checkout", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, Content(responseContent, "application/json"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API checkout.");
                return StatusCode(500, new { message = "Lỗi kết nối tới hệ thống thanh toán API." });
            }
        }

        /// <summary>
        /// Trang hiển thị chi tiết đặt vé thành công và Mã QR Code vé điện tử.
        /// GET: /Booking/Success?bookingId=1
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings/{bookingId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var bookingDetail = JsonSerializer.Deserialize<BookingSuccessApiDto>(content, JsonOptions);

                    if (bookingDetail != null)
                    {
                        var model = new BookingSuccessViewModel
                        {
                            BookingId = bookingDetail.BookingId,
                            QrCodeToken = bookingDetail.QrCodeToken,
                            TotalAmount = bookingDetail.TotalAmount,
                            Status = bookingDetail.Status,
                            PaymentStatus = bookingDetail.PaymentStatus,
                            PaymentMethod = bookingDetail.PaymentMethod,
                            TransactionId = bookingDetail.TransactionId,
                            CreatedAt = bookingDetail.CreatedAt,
                            MovieTitle = bookingDetail.Showtime.MovieTitle,
                            PosterUrl = bookingDetail.Showtime.PosterUrl,
                            RoomName = bookingDetail.Showtime.RoomName,
                            ScreenType = bookingDetail.Showtime.ScreenType,
                            StartTime = bookingDetail.Showtime.StartTime,
                            EndTime = bookingDetail.Showtime.EndTime,
                            Seats = bookingDetail.Seats.Select(s => new BookedSeatItemModel
                            {
                                SeatId = s.SeatId,
                                SeatName = s.SeatName,
                                SeatTypeName = s.SeatTypeName,
                                Price = s.Price
                            }).ToList(),
                            Combos = bookingDetail.Combos.Select(c => new BookedComboItemModel
                            {
                                ComboId = c.ComboId,
                                ComboName = c.ComboName,
                                Quantity = c.Quantity,
                                UnitPrice = c.UnitPrice
                            }).ToList(),
                            CustomerName = bookingDetail.Customer?.FullName,
                            CustomerPhone = bookingDetail.Customer?.Phone,
                            CustomerEmail = bookingDetail.Customer?.Email
                        };

                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết đơn đặt vé #{BookingId}", bookingId);
            }

            // Fallback demo model
            var fallback = new BookingSuccessViewModel
            {
                BookingId = bookingId,
                QrCodeToken = Guid.NewGuid().ToString("N"),
                TotalAmount = 219000,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                PaymentMethod = "EWallet (MoMo)",
                TransactionId = "TXN_" + DateTime.Now.Ticks,
                CreatedAt = DateTime.Now,
                MovieTitle = "Deadpool & Wolverine",
                RoomName = "Phòng Chiếu 01",
                ScreenType = "2D Digital",
                StartTime = DateTime.Today.AddHours(19),
                EndTime = DateTime.Today.AddHours(21).AddMinutes(15),
                CustomerName = "Nguyễn Văn A",
                CustomerPhone = "0912345678",
                CustomerEmail = "khachhang@gmail.com",
                Seats = new List<BookedSeatItemModel>
                {
                    new() { SeatId = 11, SeatName = "B1", SeatTypeName = "VIP", Price = 100000 },
                    new() { SeatId = 12, SeatName = "B2", SeatTypeName = "VIP", Price = 100000 }
                },
                Combos = new List<BookedComboItemModel>
                {
                    new() { ComboId = 1, ComboName = "Combo Solo", Quantity = 1, UnitPrice = 85000 }
                }
            };

            return View(fallback);
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

            // Hàng C (1-10): Ghế Couple
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

        private static List<ComboItemViewModel> CreateFallbackCombos()
        {
            return new List<ComboItemViewModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Combo Solo",
                    Description = "1 Bắp ngọt vừa (60oz) + 1 Nước ngọt có ga vừa (22oz)",
                    Price = 85000,
                    ImageUrl = "https://images.unsplash.com/photo-1585647347384-2593bc35786b?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                },
                new()
                {
                    Id = 2,
                    Name = "Combo Couple",
                    Description = "1 Bắp lớn (85oz) + 2 Nước ngọt vừa (22oz)",
                    Price = 119000,
                    ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                },
                new()
                {
                    Id = 3,
                    Name = "Combo Party Family",
                    Description = "2 Bắp phô mai lớn + 4 Nước ngọt lớn + 1 Khoai tây lắc",
                    Price = 209000,
                    ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                },
                new()
                {
                    Id = 4,
                    Name = "Combo VIP Caramel Cheese",
                    Description = "1 Bắp caramel/phô mai cao cấp + 2 Nước ép trái cây tươi",
                    Price = 139000,
                    ImageUrl = "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?auto=format&fit=crop&w=500&q=80",
                    IsActive = true
                }
            };
        }
    }

    // Helper DTOs to deserialize API response
    internal class BookingSuccessApiDto
    {
        public int BookingId { get; set; }
        public string QrCodeToken { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public ShowtimeDetailSubDto Showtime { get; set; } = new();
        public List<SeatDetailSubDto> Seats { get; set; } = new();
        public List<ComboDetailSubDto> Combos { get; set; } = new();
        public CustomerDetailSubDto? Customer { get; set; }
    }

    internal class ShowtimeDetailSubDto
    {
        public int Id { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    internal class SeatDetailSubDto
    {
        public int SeatId { get; set; }
        public string SeatName { get; set; } = string.Empty;
        public string SeatTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    internal class ComboDetailSubDto
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    internal class CustomerDetailSubDto
    {
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
