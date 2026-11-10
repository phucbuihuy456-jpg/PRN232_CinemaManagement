namespace CinemaSystemAPI.DTOs.Reports
{
    public class RevenueReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TicketRevenue { get; set; }
        public decimal ComboRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalBookingsCount { get; set; }
        public List<DailyRevenueItemDto> DailyRevenues { get; set; } = new();
    }

    public class DailyRevenueItemDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int BookingsCount { get; set; }
    }

    public class TopMovieReportDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalShowtimes { get; set; }
    }

    public class DailySummaryDto
    {
        public string Today { get; set; } = string.Empty;
        public decimal TodayRevenue { get; set; }
        public decimal YesterdayRevenue { get; set; }
        public double RevenueGrowthPercentage { get; set; }
        public int TodayTicketsSold { get; set; }
        public int TodayBookingsCount { get; set; }
        public int ActiveShowtimesCount { get; set; }
        public int NewUsersCount { get; set; }
    }

    public class OccupancyReportDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int TotalSeatsBooked { get; set; }
        public double OccupancyRatePercentage { get; set; }
        public int TotalShowtimesCount { get; set; }
    }
}

