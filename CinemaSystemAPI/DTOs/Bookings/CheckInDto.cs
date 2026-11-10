namespace CinemaSystemAPI.DTOs.Bookings
{
    public class CheckInRequestDto
    {
        public string? QrCodeToken { get; set; }
        public int? BookingId { get; set; }
    }

    public class CheckInResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string QrCodeToken { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CheckedInAt { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string Showtime { get; set; } = string.Empty;
        public List<string> Seats { get; set; } = new();
        public List<string> Combos { get; set; } = new();
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

