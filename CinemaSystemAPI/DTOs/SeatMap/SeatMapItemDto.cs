namespace CinemaSystemAPI.DTOs.SeatMap
{
    public class SeatMapItemDto
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
        /// Trạng thái ghế: Available (Trống), Held (Đang giữ), Sold (Đã bán), Disabled (Khóa/Bảo trì)
        /// </summary>
        public string Status { get; set; } = "Available";

        /// <summary>
        /// Thời điểm hết hạn giữ chỗ (nếu đang ở trạng thái Held)
        /// </summary>
        public DateTime? HoldExpiresAt { get; set; }

        /// <summary>
        /// Số giây giữ chỗ còn lại (phục vụ đếm ngược trên giao diện)
        /// </summary>
        public int? HeldSecondsRemaining { get; set; }
    }
}
