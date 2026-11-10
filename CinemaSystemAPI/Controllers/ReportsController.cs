using CinemaSystemAPI.DTOs.Reports;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(CinemaDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 1. Báo cáo doanh thu theo khoảng thời gian (?from=&to=).
        /// Hỗ trợ vẽ biểu đồ doanh thu theo từng ngày.
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueReportDto>> GetRevenueReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var fromDate = from?.Date ?? DateTime.Today.AddDays(-6); // Mặc định 7 ngày gần nhất
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

            if (fromDate > toDate)
            {
                return BadRequest(new { message = "Ngày bắt đầu không được lớn hơn ngày kết thúc!" });
            }

            // Lấy danh sách booking hợp lệ trong khoảng thời gian
            var validBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.BookingSeats)
                .Include(b => b.BookingCombos)
                .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate &&
                            (b.PaymentStatus == "Paid" || b.Status == "Confirmed" || b.Status == "Completed"))
                .ToListAsync();

            decimal totalRevenue = validBookings.Sum(b => b.TotalAmount);
            decimal ticketRevenue = validBookings.SelectMany(b => b.BookingSeats).Sum(s => s.Price);
            decimal comboRevenue = validBookings.SelectMany(b => b.BookingCombos).Sum(c => c.Quantity * c.UnitPrice);
            int totalTickets = validBookings.SelectMany(b => b.BookingSeats).Count();
            int totalBookingsCount = validBookings.Count;

            // Nhóm theo từng ngày trong khoảng thời gian
            var dailyGroup = validBookings
                .GroupBy(b => b.CreatedAt.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => new
                {
                    Revenue = g.Sum(b => b.TotalAmount),
                    Tickets = g.SelectMany(b => b.BookingSeats).Count(),
                    Bookings = g.Count()
                });

            var dailyList = new List<DailyRevenueItemDto>();
            var curDate = fromDate.Date;
            while (curDate <= toDate.Date)
            {
                var dateKey = curDate.ToString("yyyy-MM-dd");
                if (dailyGroup.TryGetValue(dateKey, out var dayData))
                {
                    dailyList.Add(new DailyRevenueItemDto
                    {
                        Date = curDate.ToString("dd/MM"),
                        Revenue = dayData.Revenue,
                        TicketsSold = dayData.Tickets,
                        BookingsCount = dayData.Bookings
                    });
                }
                else
                {
                    dailyList.Add(new DailyRevenueItemDto
                    {
                        Date = curDate.ToString("dd/MM"),
                        Revenue = 0,
                        TicketsSold = 0,
                        BookingsCount = 0
                    });
                }
                curDate = curDate.AddDays(1);
            }

            return Ok(new RevenueReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalRevenue = totalRevenue,
                TicketRevenue = ticketRevenue,
                ComboRevenue = comboRevenue,
                TotalTicketsSold = totalTickets,
                TotalBookingsCount = totalBookingsCount,
                DailyRevenues = dailyList
            });
        }

        /// <summary>
        /// 2. Báo cáo Top phim có doanh thu và số lượng vé bán chạy nhất (?limit=5).
        /// </summary>
        [HttpGet("top-movies")]
        public async Task<ActionResult<List<TopMovieReportDto>>> GetTopMovies([FromQuery] int limit = 5)
        {
            if (limit < 1 || limit > 50) limit = 5;

            var movies = await _context.Movies
                .AsNoTracking()
                .Where(m => !m.IsDeleted)
                .Include(m => m.Showtimes)
                    .ThenInclude(s => s.Bookings.Where(b => b.PaymentStatus == "Paid" || b.Status == "Confirmed" || b.Status == "Completed"))
                        .ThenInclude(b => b.BookingSeats)
                .ToListAsync();

            var topList = movies.Select(m =>
            {
                var bookings = m.Showtimes.SelectMany(s => s.Bookings).ToList();
                int ticketsSold = bookings.SelectMany(b => b.BookingSeats).Count();
                decimal revenue = bookings.Sum(b => b.TotalAmount);

                return new TopMovieReportDto
                {
                    MovieId = m.Id,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,
                    DurationMinutes = m.DurationMinutes,
                    TotalTicketsSold = ticketsSold,
                    TotalRevenue = revenue,
                    TotalShowtimes = m.Showtimes.Count
                };
            })
            .OrderByDescending(m => m.TotalRevenue)
            .ThenByDescending(m => m.TotalTicketsSold)
            .Take(limit)
            .ToList();

            return Ok(topList);
        }

        /// <summary>
        /// 3. Thống kê nhanh trong ngày (Daily Summary) phục vụ Dashboard.
        /// </summary>
        [HttpGet("daily-summary")]
        public async Task<ActionResult<DailySummaryDto>> GetDailySummary()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);

            // Doanh thu hôm nay
            var todayBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.BookingSeats)
                .Where(b => b.CreatedAt >= today && b.CreatedAt < tomorrow &&
                            (b.PaymentStatus == "Paid" || b.Status == "Confirmed" || b.Status == "Completed"))
                .ToListAsync();

            decimal todayRevenue = todayBookings.Sum(b => b.TotalAmount);
            int todayTickets = todayBookings.SelectMany(b => b.BookingSeats).Count();
            int todayBookingsCount = todayBookings.Count;

            // Doanh thu hôm qua
            var yesterdayRevenue = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= yesterday && b.CreatedAt < today &&
                            (b.PaymentStatus == "Paid" || b.Status == "Confirmed" || b.Status == "Completed"))
                .SumAsync(b => b.TotalAmount);

            double growth = 0;
            if (yesterdayRevenue > 0)
            {
                growth = Math.Round((double)((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100, 1);
            }
            else if (todayRevenue > 0)
            {
                growth = 100.0;
            }

            // Suất chiếu hoạt động hôm nay
            int activeShowtimes = await _context.Showtimes
                .AsNoTracking()
                .CountAsync(s => s.StartTime >= today && s.StartTime < tomorrow && s.IsActive);

            // Người dùng mới đăng ký hôm nay
            int newUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.CreatedAt >= today && u.CreatedAt < tomorrow);

            return Ok(new DailySummaryDto
            {
                Today = today.ToString("dd/MM/yyyy"),
                TodayRevenue = todayRevenue,
                YesterdayRevenue = yesterdayRevenue,
                RevenueGrowthPercentage = growth,
                TodayTicketsSold = todayTickets,
                TodayBookingsCount = todayBookingsCount,
                ActiveShowtimesCount = activeShowtimes,
                NewUsersCount = newUsers
            });
        }

        /// <summary>
        /// 4. Thống kê Tỷ lệ lấp đầy phòng chiếu (Occupancy Rate).
        /// </summary>
        [HttpGet("occupancy")]
        public async Task<ActionResult<List<OccupancyReportDto>>> GetOccupancyReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var fromDate = from?.Date ?? DateTime.Today.AddDays(-30);
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

            var rooms = await _context.Rooms
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Include(r => r.Showtimes.Where(s => s.StartTime >= fromDate && s.StartTime <= toDate && s.IsActive))
                    .ThenInclude(s => s.Bookings.Where(b => b.PaymentStatus == "Paid" || b.Status == "Confirmed" || b.Status == "Completed"))
                        .ThenInclude(b => b.BookingSeats)
                .ToListAsync();

            var result = rooms.Select(r =>
            {
                int showtimesCount = r.Showtimes.Count;
                int totalCapacity = showtimesCount * r.TotalSeats;
                int seatsBooked = r.Showtimes.SelectMany(s => s.Bookings).SelectMany(b => b.BookingSeats).Count();
                double rate = totalCapacity > 0 ? Math.Round(((double)seatsBooked / totalCapacity) * 100, 1) : 0;

                return new OccupancyReportDto
                {
                    RoomId = r.Id,
                    RoomName = r.Name,
                    ScreenType = r.ScreenType ?? "2D",
                    TotalCapacity = totalCapacity,
                    TotalSeatsBooked = seatsBooked,
                    OccupancyRatePercentage = rate,
                    TotalShowtimesCount = showtimesCount
                };
            }).ToList();

            return Ok(result);
        }
    }
}

