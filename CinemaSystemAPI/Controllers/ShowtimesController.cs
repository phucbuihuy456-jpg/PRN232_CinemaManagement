using CinemaSystemAPI.DTOs.SeatMap;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShowtimesController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public ShowtimesController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy sơ đồ ghế thời gian thực (Real-time Seat Map) của một suất chiếu.
        /// Tổng hợp trạng thái ghế: Available (Trống), Held (Đang giữ), Sold (Đã bán), Disabled (Khóa/Bảo trì).
        /// </summary>
        /// <param name="id">ID của suất chiếu</param>
        [HttpGet("{id:int}/seat-map")]
        public async Task<ActionResult<ShowtimeSeatMapDto>> GetSeatMap(int id)
        {
            var showtime = await _context.Showtimes
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (showtime == null)
            {
                return NotFound(new { message = $"Không tìm thấy suất chiếu với ID = {id}" });
            }

            var now = DateTime.Now;

            // 1. Lấy danh sách tất cả ghế thuộc phòng chiếu tương ứng
            var roomSeats = await _context.Seats
                .AsNoTracking()
                .Where(s => s.RoomId == showtime.RoomId)
                .Include(s => s.SeatType)
                .OrderBy(s => s.RowCode)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

            // 2. Lấy danh sách ID các ghế đã bán (thuộc Booking có trạng thái Confirmed, Completed hoặc PaymentStatus = Paid)
            var soldSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ShowtimeId == id &&
                             (bs.Booking.Status == "Confirmed" ||
                              bs.Booking.Status == "Completed" ||
                              bs.Booking.PaymentStatus == "Paid"))
                .Select(bs => bs.SeatId)
                .Distinct()
                .ToListAsync();

            var soldSeatSet = new HashSet<int>(soldSeatIds);

            // 3. Lấy danh sách SeatHolds còn hạn (ExpiresAt > now)
            var activeHolds = await _context.SeatHolds
                .AsNoTracking()
                .Where(h => h.ShowtimeId == id && h.ExpiresAt > now)
                .ToListAsync();

            var holdMap = activeHolds
                .GroupBy(h => h.SeatId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ExpiresAt).First());

            // 4. Tổng hợp trạng thái từng ghế
            var seatItems = new List<SeatMapItemDto>();
            int availableCount = 0;
            int heldCount = 0;
            int soldCount = 0;
            int disabledCount = 0;

            foreach (var seat in roomSeats)
            {
                string status;
                DateTime? holdExpiresAt = null;
                int? heldSecondsRemaining = null;

                if (!seat.IsActive || !showtime.Room.IsActive)
                {
                    status = "Disabled";
                    disabledCount++;
                }
                else if (soldSeatSet.Contains(seat.Id))
                {
                    status = "Sold";
                    soldCount++;
                }
                else if (holdMap.TryGetValue(seat.Id, out var hold))
                {
                    status = "Held";
                    holdExpiresAt = hold.ExpiresAt;
                    var remaining = (int)(hold.ExpiresAt - now).TotalSeconds;
                    heldSecondsRemaining = remaining > 0 ? remaining : 0;
                    heldCount++;
                }
                else
                {
                    status = "Available";
                    availableCount++;
                }

                var extraPrice = seat.SeatType?.ExtraPrice ?? 0;
                var finalPrice = showtime.BasePrice + extraPrice;

                seatItems.Add(new SeatMapItemDto
                {
                    SeatId = seat.Id,
                    RowCode = seat.RowCode,
                    SeatNumber = seat.SeatNumber,
                    SeatTypeId = seat.SeatTypeId,
                    SeatTypeName = seat.SeatType?.Name ?? "Standard",
                    ExtraPrice = extraPrice,
                    FinalPrice = finalPrice,
                    Status = status,
                    HoldExpiresAt = holdExpiresAt,
                    HeldSecondsRemaining = heldSecondsRemaining
                });
            }

            var result = new ShowtimeSeatMapDto
            {
                ShowtimeId = showtime.Id,
                MovieId = showtime.MovieId,
                MovieTitle = showtime.Movie.Title,
                PosterUrl = showtime.Movie.PosterUrl,
                RoomId = showtime.RoomId,
                RoomName = showtime.Room.Name,
                ScreenType = showtime.Room.ScreenType,
                StartTime = showtime.StartTime,
                EndTime = showtime.EndTime,
                BasePrice = showtime.BasePrice,
                TotalSeats = seatItems.Count,
                AvailableCount = availableCount,
                HeldCount = heldCount,
                SoldCount = soldCount,
                DisabledCount = disabledCount,
                Seats = seatItems
            };

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách suất chiếu (hỗ trợ lọc theo movieId hoặc date).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetShowtimes([FromQuery] int? movieId, [FromQuery] DateTime? date)
        {
            var query = _context.Showtimes
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .Where(s => s.IsActive);

            if (movieId.HasValue)
            {
                query = query.Where(s => s.MovieId == movieId.Value);
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                var nextDate = targetDate.AddDays(1);
                query = query.Where(s => s.StartTime >= targetDate && s.StartTime < nextDate);
            }

            var list = await query
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    s.MovieId,
                    MovieTitle = s.Movie.Title,
                    s.RoomId,
                    RoomName = s.Room.Name,
                    ScreenType = s.Room.ScreenType,
                    s.StartTime,
                    s.EndTime,
                    s.BasePrice,
                    s.IsActive
                })
                .ToListAsync();

            return Ok(list);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ShowTimeDetailDto>> GetShowtimeById(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .Where(s => s.Id == id && s.IsActive)
                .Select(s => new ShowTimeDetailDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    BasePrice = s.BasePrice,
                    IsActive = s.IsActive,
                    MovieId = s.MovieId,
                    Title = s.Movie.Title,
                    PosterUrl = s.Movie.PosterUrl,
                    AgeRating = s.Movie.AgeRating,
                    DurationMinutes = s.Movie.DurationMinutes,
                    Language = s.Movie.Language,
                    RoomId = s.RoomId,
                    RoomName = s.Room.Name,
                    ScreenType = s.Room.ScreenType,
                    TotalSeats = _context.Seats.Count(seat => seat.RoomId == s.RoomId),
                    AvailableSeats = _context.Seats.Count(seat => seat.RoomId == s.RoomId && seat.IsActive) -
                                     _context.BookingSeats.Count(bs => bs.Booking.ShowtimeId == s.Id &&
                                                                       (bs.Booking.Status == "Confirmed" ||
                                                                        bs.Booking.Status == "Completed" ||
                                                                        bs.Booking.PaymentStatus == "Paid"))
                })
                .FirstOrDefaultAsync();
            if (showtime == null)
            {
                return NotFound(new { message = $"Không tìm thấy suất chiếu với ID = {id}" });
            }
            return Ok(showtime);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Showtime>> CreateShowtime(CreateShowTimeDto showTime)
        {
            // 1. Kiểm tra thời gian bắt đầu
            if (showTime.StartTime <= DateTime.Now)
            {
                return BadRequest(new { message = "Thời gian bắt đầu suất chiếu phải ở tương lai." });
            }

            // 2. Kiểm tra giờ kết thúc phải sau giờ bắt đầu
            if (showTime.EndTime <= showTime.StartTime)
            {
                return BadRequest(new { message = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu." });
            }

            // 3. Kiểm tra giá vé cơ sở
            if (showTime.BasePrice < 0)
            {
                return BadRequest(new { message = "Giá vé cơ sở không được âm." });
            }

            // 4. Kiểm tra phim tồn tại
            var checkMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == showTime.MovieId && !m.IsDeleted);
            if (checkMovie == null)
            {
                return BadRequest(new { message = $"Phim với ID = {showTime.MovieId} không tồn tại hoặc không hoạt động." });
            }

            // 5. Kiểm tra thời lượng phim có đủ trong khung giờ không
            if (showTime.EndTime < showTime.StartTime.AddMinutes(checkMovie.DurationMinutes))
            {
                return BadRequest(new
                {
                    message = $"Khoảng thời gian chiếu không đủ cho bộ phim này (Thời lượng phim: {checkMovie.DurationMinutes} phút)."
                });
            }

            // 6. Kiểm tra ngày chiếu trong khoảng phát hành của phim
            var showDate = DateOnly.FromDateTime(showTime.StartTime);
            if (showDate < checkMovie.ReleaseDate || showDate > checkMovie.EndDate)
            {
                return BadRequest(new { message = $"Ngày chiếu không nằm trong khoảng phát hành của phim ({checkMovie.ReleaseDate} - {checkMovie.EndDate})." });
            }

            // 7. Kiểm tra phòng chiếu
            var checkRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == showTime.RoomId && r.IsActive);
            if (checkRoom == null)
            {
                return BadRequest(new { message = $"Phòng chiếu với ID = {showTime.RoomId} không tồn tại hoặc không hoạt động." });
            }

            // 8. Validate trùng giờ
            bool isOverlapped = await _context.Showtimes.AnyAsync(s =>
                s.RoomId == showTime.RoomId &&
                s.IsActive &&
                showTime.StartTime < s.EndTime &&
                showTime.EndTime > s.StartTime
            );

            if (isOverlapped)
            {
                return BadRequest(new
                {
                    message = "Phòng chiếu này đã có suất chiếu khác trong khoảng thời gian đã chọn! Vui lòng chọn giờ hoặc phòng khác."
                });
            }

            var newShowtime = new Showtime
            {
                MovieId = showTime.MovieId,
                RoomId = showTime.RoomId,
                StartTime = showTime.StartTime,
                EndTime = showTime.EndTime,
                BasePrice = showTime.BasePrice,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Showtimes.Add(newShowtime);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShowtimeById), new { id = newShowtime.Id }, newShowtime);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Showtime>> UpdateShowTime(int id, UpdateShowTimeDto showTime)
        {
            // 1. TÌM SUẤT CHIẾU CẦN SỬA TRƯỚC TIÊN
            var existingShowtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
            if (existingShowtime == null)
            {
                return NotFound(new { message = $"Không tìm thấy suất chiếu với ID = {id}" });
            }

            // 2. Không cho phép sửa suất chiếu đã hoặc đang diễn ra
            if (existingShowtime.StartTime < DateTime.Now)
            {
                return BadRequest(new { message = "Không thể chỉnh sửa suất chiếu đã hoặc đang diễn ra!" });
            }

            // 3. Kiểm tra nếu đã có vé bán thì không cho đổi Phim hoặc Phòng chiếu
            bool hasTicketsSold = await _context.BookingSeats.AnyAsync(bs =>
                bs.Booking.ShowtimeId == id &&
                (bs.Booking.Status == "Confirmed" || bs.Booking.Status == "Completed" || bs.Booking.PaymentStatus == "Paid")
            );

            if (hasTicketsSold)
            {
                if (existingShowtime.MovieId != showTime.MovieId || existingShowtime.RoomId != showTime.RoomId)
                {
                    return BadRequest(new
                    {
                        message = "Suất chiếu này đã có khách mua vé, không thể thay đổi Phim hoặc Phòng chiếu!"
                    });
                }
            }

            // 4. Validate thời gian & giá vé
            if (showTime.StartTime <= DateTime.Now)
            {
                return BadRequest(new { message = "Thời gian bắt đầu suất chiếu phải ở tương lai." });
            }

            if (showTime.EndTime <= showTime.StartTime)
            {
                return BadRequest(new { message = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu." });
            }

            if (showTime.BasePrice < 0)
            {
                return BadRequest(new { message = "Giá vé cơ sở không được âm." });
            }

            // 5. Kiểm tra Phim (SỬA LẠI: dùng showTime.MovieId)
            var checkMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == showTime.MovieId && !m.IsDeleted);
            if (checkMovie == null)
            {
                return BadRequest(new { message = $"Phim với ID = {showTime.MovieId} không tồn tại hoặc không hoạt động." });
            }

            // 6. Kiểm tra thời lượng phim
            if (showTime.EndTime < showTime.StartTime.AddMinutes(checkMovie.DurationMinutes))
            {
                return BadRequest(new
                {
                    message = $"Khoảng thời gian chiếu không đủ cho bộ phim này (Thời lượng phim: {checkMovie.DurationMinutes} phút)."
                });
            }

            // 7. Kiểm tra ngày chiếu trong khoảng phát hành của phim
            var showDate = DateOnly.FromDateTime(showTime.StartTime);
            if (showDate < checkMovie.ReleaseDate || showDate > checkMovie.EndDate)
            {
                return BadRequest(new { message = $"Ngày chiếu không nằm trong khoảng phát hành của phim ({checkMovie.ReleaseDate} - {checkMovie.EndDate})." });
            }

            // 8. Kiểm tra phòng chiếu
            var checkRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == showTime.RoomId && r.IsActive);
            if (checkRoom == null)
            {
                return BadRequest(new { message = $"Phòng chiếu với ID = {showTime.RoomId} không tồn tại hoặc không hoạt động." });
            }

            // 9. Validate trùng giờ (có loại trừ chính nó: s.Id != id)
            bool isOverlapped = await _context.Showtimes.AnyAsync(s =>
                s.RoomId == showTime.RoomId &&
                s.IsActive &&
                s.Id != id &&
                showTime.StartTime < s.EndTime &&
                showTime.EndTime > s.StartTime
            );

            if (isOverlapped)
            {
                return BadRequest(new
                {
                    message = "Phòng chiếu này đã có suất chiếu khác trong khoảng thời gian đã chọn! Vui lòng chọn giờ hoặc phòng khác."
                });
            }

            // 10. Cập nhật dữ liệu
            existingShowtime.MovieId = showTime.MovieId;
            existingShowtime.RoomId = showTime.RoomId;
            existingShowtime.StartTime = showTime.StartTime;
            existingShowtime.EndTime = showTime.EndTime;
            existingShowtime.BasePrice = showTime.BasePrice;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật suất chiếu thành công!" });
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<Showtime>> DeleteShowTime(int id)
        {
            var checkShowTime = await _context.Showtimes
                .Include(s => s.SeatHolds)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive == true);
            if (checkShowTime == null)
            {
                return NotFound(new { message = "Không tìm thấy showtime" });
            }
            if (checkShowTime.StartTime <= DateTime.Now)
            {
                return BadRequest(new { message = "Không thể xóa suất chiếu đã/đang diễn ra" });
            }
            bool hasTicketsSold = await _context.BookingSeats.AnyAsync(bs =>
        bs.Booking.ShowtimeId == id &&
        (bs.Booking.Status == "Confirmed" ||
         bs.Booking.Status == "Completed" ||
         bs.Booking.PaymentStatus == "Paid")
    );
            if (hasTicketsSold)
            {
                return BadRequest(new
                {
                    message = "Suất chiếu này đã có khách hàng mua vé, không thể xóa! Vui lòng xử lý hủy vé/hoàn tiền trước."
                });
            }
            // 4. Dọn dẹp các ghế đang được giữ tạm (nếu có ai đang chọn ghế dở dang)
            if (checkShowTime.SeatHolds != null && checkShowTime.SeatHolds.Any())
            {
                _context.SeatHolds.RemoveRange(checkShowTime.SeatHolds);
            }
            // 5. Thực hiện xóa mềm (Soft Delete)
            checkShowTime.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa suất chiếu thành công!" });
        }
    }
}
