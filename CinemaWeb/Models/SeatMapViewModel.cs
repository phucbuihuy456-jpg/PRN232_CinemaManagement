namespace CinemaWeb.Models
{
    public class SeatItemViewModel
    {
        public int SeatId { get; set; }
        public string RowCode { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string SeatName => $"{RowCode}{SeatNumber}";
        public int SeatTypeId { get; set; }
        public string SeatTypeName { get; set; } = null!;
        public decimal ExtraPrice { get; set; }
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Available, Held, Sold, Disabled
        /// </summary>
        public string Status { get; set; } = "Available";
        public DateTime? HoldExpiresAt { get; set; }
        public int? HeldSecondsRemaining { get; set; }
    }

    public class SeatMapViewModel
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
        public List<SeatItemViewModel> Seats { get; set; } = new List<SeatItemViewModel>();
        public string? ErrorMessage { get; set; }
    }
}
