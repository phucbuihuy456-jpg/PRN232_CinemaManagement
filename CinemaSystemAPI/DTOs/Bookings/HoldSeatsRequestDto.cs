using System.ComponentModel.DataAnnotations;

namespace CinemaSystemAPI.DTOs.Bookings
{
    /// <summary>
    /// DTO yêu cầu giữ ghế tạm thời trong 5 phút.
    /// </summary>
    public class HoldSeatsRequestDto
    {
        /// <summary>
        /// ID của suất chiếu.
        /// </summary>
        [Required(ErrorMessage = "ShowtimeId là bắt buộc.")]
        public int ShowtimeId { get; set; }

        /// <summary>
        /// Danh sách ID các ghế muốn giữ tạm (tối đa 8 ghế).
        /// </summary>
        [Required(ErrorMessage = "Danh sách ghế không được để trống.")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế.")]
        public List<int> SeatIds { get; set; } = new();

        /// <summary>
        /// Mã phiên giữ ghế cũ nếu muốn cập nhật / thay thế ghế đã giữ trước đó.
        /// </summary>
        public string? HoldToken { get; set; }

        /// <summary>
        /// ID khách hàng nếu đã đăng nhập (hoặc lấy từ JWT token).
        /// </summary>
        public int? UserId { get; set; }
    }
}
