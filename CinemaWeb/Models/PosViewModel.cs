namespace CinemaWeb.Models
{
    public class PosViewModel
    {
        public List<PosShowtimeItem> Showtimes { get; set; } = new();
        public List<ComboItemViewModel> Combos { get; set; } = new();
    }

    public class PosShowtimeItem
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
    }

    public class PosCheckoutRequestModel
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new();
        public List<PosComboItemModel>? Combos { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal CashReceived { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class PosComboItemModel
    {
        public int ComboId { get; set; }
        public int Quantity { get; set; }
    }
}
