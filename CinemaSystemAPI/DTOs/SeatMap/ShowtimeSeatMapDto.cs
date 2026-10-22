namespace CinemaSystemAPI.DTOs.SeatMap
{
    public class ShowtimeSeatMapDto
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public string? PosterUrl { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public string? ScreenType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableCount { get; set; }
        public int HeldCount { get; set; }
        public int SoldCount { get; set; }
        public int DisabledCount { get; set; }
        public List<SeatMapItemDto> Seats { get; set; } = new List<SeatMapItemDto>();
    }
}
