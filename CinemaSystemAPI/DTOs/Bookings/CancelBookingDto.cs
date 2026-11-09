namespace CinemaSystemAPI.DTOs.Bookings
{
    /// <summary>
    /// DTO gửi yêu cầu hủy đơn vé.
    /// </summary>
    public class CancelBookingRequestDto
    {
        /// <summary>
        /// Lý do hủy vé (tùy chọn).
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO phản hồi sau khi hủy đơn vé và hoàn ghế thành công.
    /// </summary>
    public class CancelBookingResponseDto
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = "Cancelled";
        public string PaymentStatus { get; set; } = "Refunded";
        public decimal RefundAmount { get; set; }
        public DateTime CancelledAt { get; set; }
        public List<string> FreedSeatNames { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
