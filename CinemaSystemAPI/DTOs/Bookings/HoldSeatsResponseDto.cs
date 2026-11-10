namespace CinemaSystemAPI.DTOs.Bookings
{
    /// <summary>
    /// DTO phản hồi sau khi giữ ghế tạm thành công.
    /// </summary>
    public class HoldSeatsResponseDto
    {
        /// <summary>
        /// Mã token định danh phiên giữ ghế (dùng khi checkout).
        /// </summary>
        public string HoldToken { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm hết hạn giữ ghế (sau 5 phút).
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Số giây còn lại cho đến khi hết hạn (mặc định 300s = 5 phút).
        /// </summary>
        public int RemainingSeconds { get; set; }

        /// <summary>
        /// ID của suất chiếu.
        /// </summary>
        public int ShowtimeId { get; set; }

        /// <summary>
        /// Danh sách ID các ghế đã được giữ thành công.
        /// </summary>
        public List<int> HeldSeatIds { get; set; } = new();

        /// <summary>
        /// Tên các ghế đã giữ (ví dụ: A1, A2).
        /// </summary>
        public List<string> HeldSeatNames { get; set; } = new();

        /// <summary>
        /// Tổng tiền vé tạm tính của các ghế đã giữ.
        /// </summary>
        public decimal TotalSeatPrice { get; set; }

        /// <summary>
        /// Thông điệp phản hồi.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
