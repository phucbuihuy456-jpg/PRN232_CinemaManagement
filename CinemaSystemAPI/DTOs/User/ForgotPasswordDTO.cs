using System.ComponentModel.DataAnnotations;

namespace CinemaSystemAPI.DTOs.User
{
    public class ForgotPasswordRequestDTO
    {
        [Required(ErrorMessage = "Email không được để trống!")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ!")]
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordOtpDTO
    {
        [Required(ErrorMessage = "Email không được để trống!")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ!")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mã OTP không được để trống!")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số!")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ bao gồm các chữ số từ 0-9!")]
        public string Otp { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống!")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống!")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp!")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
