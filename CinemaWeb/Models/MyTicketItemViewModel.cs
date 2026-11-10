namespace CinemaWeb.Models
{
    public class MyTicketItemViewModel
    {
        public int Id { get; set; }
        public string? QrCodeToken { get; set; }
        public string? QrCodeBase64 { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Seats { get; set; } = new();
        public List<string> Combos { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
    }
}

