using System.ComponentModel.DataAnnotations;

namespace CinemaSystemAPI.DTOs.Bookings
{
    /// <summary>
    /// DTO yêu cầu checkout và thanh toán đơn đặt vé.
    /// </summary>
    public class CheckoutRequestDto
    {
        /// <summary>
        /// ID của suất chiếu.
        /// </summary>
        [Required(ErrorMessage = "ShowtimeId là bắt buộc.")]
        public int ShowtimeId { get; set; }

        /// <summary>
        /// Mã token giữ ghế đã nhận từ API hold-seats.
        /// </summary>
        [Required(ErrorMessage = "HoldToken là bắt buộc để xác thực quyền đặt ghế.")]
        public string HoldToken { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách ID ghế cần đặt vé (phải khớp với danh sách ghế đang giữ).
        /// </summary>
        [Required(ErrorMessage = "Danh sách ghế không được để trống.")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế.")]
        public List<int> SeatIds { get; set; } = new();

        /// <summary>
        /// Danh sách combo bắp nước chọn thêm (nếu có).
        /// </summary>
        public List<CheckoutComboItemDto>? Combos { get; set; }

        /// <summary>
        /// Phương thức thanh toán: EWallet, Cash, CreditCard, BankTransfer.
        /// </summary>
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = "EWallet";

        /// <summary>
        /// ID khách hàng nếu đã đăng nhập (hoặc null nếu mua tại quầy/khách vãng lai).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Họ và tên khách hàng nhận vé.
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Số điện thoại nhận mã vé.
        /// </summary>
        public string? CustomerPhone { get; set; }

        /// <summary>
        /// Email nhận vé điện tử & QR Code.
        /// </summary>
        public string? CustomerEmail { get; set; }
    }

    /// <summary>
    /// Thông tin từng combo trong đơn đặt vé.
    /// </summary>
    public class CheckoutComboItemDto
    {
        [Required]
        public int ComboId { get; set; }

        [Range(1, 99, ErrorMessage = "Số lượng combo phải từ 1 đến 99.")]
        public int Quantity { get; set; }
    }
}
