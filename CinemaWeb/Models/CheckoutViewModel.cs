namespace CinemaWeb.Models
{
    public class CheckoutViewModel
    {
        public int ShowtimeId { get; set; }
        public string HoldToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int RemainingSeconds { get; set; } = 300;

        // Thông tin phim và suất chiếu
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }

        // Danh sách ghế đang giữ
        public List<SeatItemViewModel> SelectedSeats { get; set; } = new();
        public decimal TotalSeatPrice => SelectedSeats.Sum(s => s.FinalPrice);

        // Danh sách combo bắp nước khả dụng
        public List<ComboItemViewModel> AvailableCombos { get; set; } = new();

        // Thông tin khách hàng
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string PaymentMethod { get; set; } = "EWallet";

        public string? ErrorMessage { get; set; }
    }

    public class ComboItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
