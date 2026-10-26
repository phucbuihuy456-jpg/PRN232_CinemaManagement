using CinemaSystemAPI.DTOs.SeatMap;
using CinemaSystemAPI.Models;
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
    }
}
