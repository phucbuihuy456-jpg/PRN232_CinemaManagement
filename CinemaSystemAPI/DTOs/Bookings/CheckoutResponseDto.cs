namespace CinemaSystemAPI.DTOs.Bookings
{
    /// <summary>
    /// DTO phản hồi sau khi checkout đặt vé thành công.
    /// </summary>
    public class CheckoutResponseDto
    {
        public int BookingId { get; set; }

        /// <summary>
        /// Mã token QR Code dùng để check-in tại rạp.
        /// </summary>
        public string QrCodeToken { get; set; } = string.Empty;

        /// <summary>
        /// Tổng số tiền thanh toán (vé + combo).
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Trạng thái đơn đặt vé (Confirmed, Completed).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái thanh toán (Paid).
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Phương thức thanh toán đã sử dụng.
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Mã giao dịch thanh toán.
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Thời điểm đặt vé thành công.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Thông tin tóm tắt suất chiếu và phim.
        /// </summary>
        public ShowtimeSummaryDto Showtime { get; set; } = new();

        /// <summary>
        /// Danh sách ghế đã đặt cùng giá tiền.
        /// </summary>
        public List<BookedSeatDetailDto> Seats { get; set; } = new();

        /// <summary>
        /// Danh sách combo bắp nước đã đặt.
        /// </summary>
        public List<BookedComboDetailDto> Combos { get; set; } = new();

        /// <summary>
        /// Thông tin khách hàng đặt vé.
        /// </summary>
        public CustomerSummaryDto? Customer { get; set; }

        /// <summary>
        /// Thông báo kết quả.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    public class ShowtimeSummaryDto
    {
        public int Id { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class BookedSeatDetailDto
    {
        public int SeatId { get; set; }
        public string SeatName { get; set; } = string.Empty;
        public string RowCode { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class BookedComboDetailDto
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CustomerSummaryDto
    {
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
