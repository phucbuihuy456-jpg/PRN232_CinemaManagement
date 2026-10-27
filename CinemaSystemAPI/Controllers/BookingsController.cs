using System.Security.Claims;
using CinemaSystemAPI.DTOs.Bookings;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(CinemaDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Task 19: Giữ ghế tạm trong vòng 5 phút (Hold Seats API).
        /// Kiểm tra ghế trống -> Thêm bản ghi vào SeatHolds với ExpiresAt = NOW + 5 phút.
        /// Trả về 409 Conflict nếu ghế đã bán hoặc đang bị người khác giữ.
        /// </summary>
        /// <param name="request">Thông tin suất chiếu và danh sách ID ghế cần giữ</param>
        /// <returns>HoldToken và thời gian hết hạn (ExpiresAt)</returns>
        [HttpPost("hold-seats")]
        public async Task<ActionResult<HoldSeatsResponseDto>> HoldSeats([FromBody] HoldSeatsRequestDto request)
        {
            if (request == null || request.SeatIds == null || !request.SeatIds.Any())
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất 1 ghế để giữ chỗ." });
            }

            if (request.SeatIds.Count > 8)
            {
                return BadRequest(new { message = "Mỗi lượt đặt vé chỉ được chọn tối đa 8 ghế." });
            }

            // 1. Kiểm tra suất chiếu tồn tại và đang hoạt động
            var showtime = await _context.Showtimes
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsActive);

            if (showtime == null)
            {
                return NotFound(new { message = $"Không tìm thấy suất chiếu với ID = {request.ShowtimeId}" });
            }

            if (showtime.EndTime <= DateTime.Now)
            {
                return BadRequest(new { message = "Suất chiếu này đã kết thúc, không thể thực hiện giữ ghế." });
            }

            // 2. Lấy thông tin các ghế từ CSDL và kiểm tra thuộc phòng chiếu
            var targetSeats = await _context.Seats
                .Include(s => s.SeatType)
                .Where(s => request.SeatIds.Contains(s.Id) && s.RoomId == showtime.RoomId && s.IsActive)
                .ToListAsync();

            if (targetSeats.Count != request.SeatIds.Distinct().Count())
            {
                return BadRequest(new { message = "Một số ghế được chọn không tồn tại hoặc không thuộc phòng chiếu của suất chiếu này." });
            }

            // 3. Kiểm tra các ghế đã được bán chưa (Booking có trạng thái Confirmed, Completed hoặc Paid)
            var soldSeats = await _context.BookingSeats
                .Include(bs => bs.Seat)
                .Where(bs => bs.Booking.ShowtimeId == request.ShowtimeId &&
                             request.SeatIds.Contains(bs.SeatId) &&
                             (bs.Booking.Status == "Confirmed" ||
                              bs.Booking.Status == "Completed" ||
                              bs.Booking.PaymentStatus == "Paid"))
                .Select(bs => $"{bs.Seat.RowCode}{bs.Seat.SeatNumber}")
                .ToListAsync();

            if (soldSeats.Any())
            {
                var seatListStr = string.Join(", ", soldSeats);
                return Conflict(new { message = $"Ghế [{seatListStr}] đã được bán thành công cho khách hàng khác. Vui lòng chọn ghế khác." });
            }

            // 4. Kiểm tra các ghế đang bị người khác giữ tạm (SeatHolds còn hạn)
            var now = DateTime.Now;
            var heldQuery = _context.SeatHolds
                .Include(h => h.Seat)
                .Where(h => h.ShowtimeId == request.ShowtimeId &&
                            request.SeatIds.Contains(h.SeatId) &&
                            h.ExpiresAt > now);

            // Nếu client gửi kèm HoldToken trước đó, bỏ qua ghế thuộc chính session này (cho phép gia hạn/đổi ghế)
            if (!string.IsNullOrEmpty(request.HoldToken))
            {
                heldQuery = heldQuery.Where(h => h.SessionId != request.HoldToken);
            }

            var conflictingHolds = await heldQuery
                .Select(h => $"{h.Seat.RowCode}{h.Seat.SeatNumber}")
                .ToListAsync();

            if (conflictingHolds.Any())
            {
                var heldStr = string.Join(", ", conflictingHolds.Distinct());
                return Conflict(new { message = $"Ghế [{heldStr}] hiện đang được người khác giữ chỗ. Vui lòng chọn ghế khác hoặc chờ hết thời gian giữ." });
            }

            // 5. Nếu client đã có HoldToken cũ, dọn dẹp các ghế cũ của session này
            string holdToken = !string.IsNullOrWhiteSpace(request.HoldToken)
                ? request.HoldToken
                : Guid.NewGuid().ToString("N");

            var previousHolds = await _context.SeatHolds
                .Where(h => h.ShowtimeId == request.ShowtimeId && h.SessionId == holdToken)
                .ToListAsync();

            if (previousHolds.Any())
            {
                _context.SeatHolds.RemoveRange(previousHolds);
            }

            // 6. Xác định UserId (từ JWT claim hoặc từ DTO)
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }
            else if (request.UserId.HasValue)
            {
                userId = request.UserId.Value;
            }

            // 7. Tạo bản ghi SeatHolds mới (ExpiresAt = NOW + 5 phút = 300 giây)
            var expiresAt = DateTime.Now.AddMinutes(5);
            var newSeatHolds = targetSeats.Select(seat => new SeatHold
            {
                ShowtimeId = request.ShowtimeId,
                SeatId = seat.Id,
                UserId = userId,
                SessionId = holdToken,
                HeldAt = DateTime.Now,
                ExpiresAt = expiresAt
            }).ToList();

            _context.SeatHolds.AddRange(newSeatHolds);
            await _context.SaveChangesAsync();

            // 8. Tính tổng tiền ghế tạm tính
            decimal totalSeatPrice = targetSeats.Sum(s => showtime.BasePrice + s.SeatType.ExtraPrice);

            return Ok(new HoldSeatsResponseDto
            {
                HoldToken = holdToken,
                ExpiresAt = expiresAt,
                RemainingSeconds = 300,
                ShowtimeId = request.ShowtimeId,
                HeldSeatIds = targetSeats.Select(s => s.Id).ToList(),
                HeldSeatNames = targetSeats.Select(s => $"{s.RowCode}{s.SeatNumber}").ToList(),
                TotalSeatPrice = totalSeatPrice,
                Message = "Giữ ghế thành công trong 5 phút. Vui lòng hoàn tất thanh toán trước khi hết hạn."
            });
        }

        /// <summary>
        /// Task 22: API Checkout Đặt vé với Database Transaction.
        /// Verify SeatHolds còn hạn -> BEGIN TRANSACTION -> INSERT Booking, BookingSeats, BookingCombos, Payment -> DELETE SeatHolds -> COMMIT.
        /// </summary>
        /// <param name="request">Thông tin đơn đặt vé, holdToken, danh sách ghế, combo và phương thức thanh toán</param>
        /// <returns>Thông tin chi tiết đơn vé đã tạo thành công cùng mã QR Code</returns>
        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutResponseDto>> Checkout([FromBody] CheckoutRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Dữ liệu checkout không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(request.HoldToken))
            {
                return BadRequest(new { message = "Thiếu mã HoldToken xác thực quyền đặt ghế." });
            }

            if (request.SeatIds == null || !request.SeatIds.Any())
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất 1 ghế." });
            }

            // 1. Kiểm tra suất chiếu
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsActive);

            if (showtime == null)
            {
                return NotFound(new { message = $"Không tìm thấy suất chiếu với ID = {request.ShowtimeId}" });
            }

            // 2. Xác thực SeatHolds còn hạn và khớp với HoldToken
            var now = DateTime.Now;
            var heldRecords = await _context.SeatHolds
                .Include(h => h.Seat)
                    .ThenInclude(s => s.SeatType)
                .Where(h => h.ShowtimeId == request.ShowtimeId &&
                            h.SessionId == request.HoldToken &&
                            request.SeatIds.Contains(h.SeatId))
                .ToListAsync();

            var validHolds = heldRecords.Where(h => h.ExpiresAt > now).ToList();

            // Nếu số lượng ghế giữ hợp lệ không đủ so với danh sách ghế muốn checkout
            if (validHolds.Count != request.SeatIds.Distinct().Count())
            {
                return Conflict(new
                {
                    message = "Thời gian giữ ghế 5 phút của bạn đã hết hạn hoặc ghế không hợp lệ. Vui lòng quay lại chọn ghế mới."
                });
            }

            // 3. Chuẩn bị thông tin ghế và tính tiền vé
            var selectedSeats = validHolds.Select(h => h.Seat).DistinctBy(s => s.Id).ToList();
            decimal totalSeatPrice = selectedSeats.Sum(s => showtime.BasePrice + s.SeatType.ExtraPrice);

            // 4. Chuẩn bị thông tin combo và tính tiền combo
            decimal totalComboPrice = 0;
            var comboOrderList = new List<(Combo combo, int qty)>();

            if (request.Combos != null && request.Combos.Any())
            {
                var requestedComboIds = request.Combos.Select(c => c.ComboId).Distinct().ToList();
                var dbCombos = await _context.Combos
                    .Where(c => requestedComboIds.Contains(c.Id) && c.IsActive)
                    .ToDictionaryAsync(c => c.Id);

                foreach (var item in request.Combos.Where(c => c.Quantity > 0))
                {
                    if (dbCombos.TryGetValue(item.ComboId, out var combo))
                    {
                        totalComboPrice += combo.Price * item.Quantity;
                        comboOrderList.Add((combo, item.Quantity));
                    }
                }
            }

            decimal totalAmount = totalSeatPrice + totalComboPrice;

            // 5. Xác định UserId (nếu có đăng nhập)
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }
            else if (request.UserId.HasValue)
            {
                userId = request.UserId.Value;
            }

            // 6. THỰC HIỆN DATABASE TRANSACTION ĐẢM BẢO TÍNH TOÀN VẸN (ACID)
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Kiểm tra lại lần cuối trong transaction để phòng ngừa Double-Booking tuyệt đối
                var alreadyBooked = await _context.BookingSeats
                    .AnyAsync(bs => bs.Booking.ShowtimeId == request.ShowtimeId &&
                                    request.SeatIds.Contains(bs.SeatId) &&
                                    (bs.Booking.Status == "Confirmed" ||
                                     bs.Booking.Status == "Completed" ||
                                     bs.Booking.PaymentStatus == "Paid"));

                if (alreadyBooked)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { message = "Một trong những ghế bạn chọn vừa được thanh toán thành công bởi giao dịch khác." });
                }

                // 6.1 Tạo đơn đặt vé mới (Booking)
                string qrCodeToken = Guid.NewGuid().ToString("N");
                var booking = new Booking
                {
                    UserId = userId,
                    ShowtimeId = request.ShowtimeId,
                    TotalAmount = totalAmount,
                    Status = "Confirmed",
                    PaymentStatus = "Paid",
                    QrCodeToken = qrCodeToken,
                    CreatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(); // Sinh Booking.Id

                // 6.2 Thêm danh sách BookingSeats
                var bookingSeats = selectedSeats.Select(seat => new BookingSeat
                {
                    BookingId = booking.Id,
                    SeatId = seat.Id,
                    Price = showtime.BasePrice + seat.SeatType.ExtraPrice
                }).ToList();

                _context.BookingSeats.AddRange(bookingSeats);

                // 6.3 Thêm danh sách BookingCombos (nếu có)
                if (comboOrderList.Any())
                {
                    var bookingCombos = comboOrderList.Select(item => new BookingCombo
                    {
                        BookingId = booking.Id,
                        ComboId = item.combo.Id,
                        Quantity = item.qty,
                        UnitPrice = item.combo.Price
                    }).ToList();

                    _context.BookingCombos.AddRange(bookingCombos);
                }

                // 6.4 Thêm bản ghi Payment
                string transactionId = "TXN_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + booking.Id;
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = totalAmount,
                    PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "EWallet" : request.PaymentMethod,
                    TransactionId = transactionId,
                    Status = "Success",
                    GatewayResponse = $"{{\"status\":\"SUCCESS\",\"method\":\"{request.PaymentMethod}\",\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}",
                    PaidAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);

                // 6.5 XÓA BẢN GHI SEAT HOLDS ĐÃ ĐẶT THÀNH CÔNG
                _context.SeatHolds.RemoveRange(validHolds);

                // 6.6 Lưu toàn bộ thay đổi vào CSDL
                await _context.SaveChangesAsync();

                // 6.7 COMMIT TRANSACTION THÀNH CÔNG
                await transaction.CommitAsync();

                _logger.LogInformation("Checkout thành công Booking #{BookingId} cho suất chiếu {ShowtimeId}, số tiền: {Amount:N0} đ",
                    booking.Id, request.ShowtimeId, totalAmount);

                // 7. Xây dựng DTO phản hồi chi tiết
                var response = new CheckoutResponseDto
                {
                    BookingId = booking.Id,
                    QrCodeToken = qrCodeToken,
                    TotalAmount = totalAmount,
                    Status = booking.Status,
                    PaymentStatus = booking.PaymentStatus,
                    PaymentMethod = payment.PaymentMethod,
                    TransactionId = payment.TransactionId,
                    CreatedAt = booking.CreatedAt,
                    Showtime = new ShowtimeSummaryDto
                    {
                        Id = showtime.Id,
                        MovieTitle = showtime.Movie.Title,
                        PosterUrl = showtime.Movie.PosterUrl,
                        RoomName = showtime.Room.Name,
                        ScreenType = showtime.Room.ScreenType,
                        StartTime = showtime.StartTime,
                        EndTime = showtime.EndTime,
                        BasePrice = showtime.BasePrice
                    },
                    Seats = selectedSeats.Select(s => new BookedSeatDetailDto
                    {
                        SeatId = s.Id,
                        SeatName = $"{s.RowCode}{s.SeatNumber}",
                        RowCode = s.RowCode,
                        SeatNumber = s.SeatNumber,
                        SeatTypeName = s.SeatType.Name,
                        Price = showtime.BasePrice + s.SeatType.ExtraPrice
                    }).ToList(),
                    Combos = comboOrderList.Select(c => new BookedComboDetailDto
                    {
                        ComboId = c.combo.Id,
                        ComboName = c.combo.Name,
                        Quantity = c.qty,
                        UnitPrice = c.combo.Price
                    }).ToList(),
                    Customer = new CustomerSummaryDto
                    {
                        UserId = userId,
                        FullName = request.CustomerName,
                        Phone = request.CustomerPhone,
                        Email = request.CustomerEmail
                    },
                    Message = "Đặt vé và thanh toán thành công!"
                };

                return Ok(response);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột đồng thời (Optimistic Concurrency) khi checkout đơn vé {ShowtimeId}", request.ShowtimeId);
                return Conflict(new { message = "Hệ thống đang có giao dịch đồng thời trên các ghế này. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra trong Transaction Checkout cho suất chiếu {ShowtimeId}", request.ShowtimeId);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Đã xảy ra sự cố trong quá trình thanh toán. Toàn bộ giao dịch đã được hoàn tác an toàn."
                });
            }
        }

        /// <summary>
        /// Giải phóng các ghế đang giữ nếu người dùng hủy hoặc không tiếp tục đặt vé.
        /// </summary>
        /// <param name="holdToken">Mã token giữ ghế</param>
        [HttpDelete("hold-seats/{holdToken}")]
        public async Task<IActionResult> ReleaseSeats(string holdToken)
        {
            if (string.IsNullOrWhiteSpace(holdToken))
            {
                return BadRequest(new { message = "Thiếu holdToken." });
            }

            var holds = await _context.SeatHolds
                .Where(h => h.SessionId == holdToken)
                .ToListAsync();

            if (holds.Any())
            {
                _context.SeatHolds.RemoveRange(holds);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Đã giải phóng {holds.Count} ghế đang giữ." });
            }

            return Ok(new { message = "Không tìm thấy ghế nào đang giữ với token này." });
        }

        /// <summary>
        /// Lấy chi tiết đơn đặt vé theo ID (phục vụ hiển thị hóa đơn / vé điện tử).
        /// </summary>
        /// <param name="id">ID đơn đặt vé</param>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CheckoutResponseDto>> GetBookingById(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Room)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(b => b.BookingCombos)
                    .ThenInclude(bc => bc.Combo)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn đặt vé #{id}" });
            }

            var latestPayment = booking.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

            var response = new CheckoutResponseDto
            {
                BookingId = booking.Id,
                QrCodeToken = booking.QrCodeToken ?? string.Empty,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = latestPayment?.PaymentMethod ?? "EWallet",
                TransactionId = latestPayment?.TransactionId,
                CreatedAt = booking.CreatedAt,
                Showtime = new ShowtimeSummaryDto
                {
                    Id = booking.Showtime.Id,
                    MovieTitle = booking.Showtime.Movie.Title,
                    PosterUrl = booking.Showtime.Movie.PosterUrl,
                    RoomName = booking.Showtime.Room.Name,
                    ScreenType = booking.Showtime.Room.ScreenType,
                    StartTime = booking.Showtime.StartTime,
                    EndTime = booking.Showtime.EndTime,
                    BasePrice = booking.Showtime.BasePrice
                },
                Seats = booking.BookingSeats.Select(bs => new BookedSeatDetailDto
                {
                    SeatId = bs.SeatId,
                    SeatName = $"{bs.Seat.RowCode}{bs.Seat.SeatNumber}",
                    RowCode = bs.Seat.RowCode,
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatTypeName = bs.Seat.SeatType.Name,
                    Price = bs.Price
                }).ToList(),
                Combos = booking.BookingCombos.Select(bc => new BookedComboDetailDto
                {
                    ComboId = bc.ComboId,
                    ComboName = bc.Combo.Name,
                    Quantity = bc.Quantity,
                    UnitPrice = bc.UnitPrice
                }).ToList(),
                Customer = new CustomerSummaryDto
                {
                    UserId = booking.UserId,
                    FullName = booking.User?.FullName,
                    Phone = booking.User?.PhoneNumber,
                    Email = booking.User?.Email
                },
                Message = "Chi tiết đơn đặt vé."
            };

            return Ok(response);
        }
    }
}
