namespace CinemaWeb.Models
{
    public class BookingSuccessViewModel
    {
        public int BookingId { get; set; }
        public string QrCodeToken { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }

        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<BookedSeatItemModel> Seats { get; set; } = new();
        public List<BookedComboItemModel> Combos { get; set; } = new();

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class BookedSeatItemModel
    {
        public int SeatId { get; set; }
        public string SeatName { get; set; } = string.Empty;
        public string SeatTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class BookedComboItemModel
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
