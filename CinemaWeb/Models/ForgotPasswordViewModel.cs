using System.ComponentModel.DataAnnotations;

namespace CinemaWeb.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email của bạn!")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ!")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordOtpViewModel
    {
        [Required(ErrorMessage = "Email không được để trống!")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ!")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mã OTP!")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số!")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ bao gồm 6 chữ số từ 0-9!")]
        [Display(Name = "Mã xác thực OTP")]
        public string Otp { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới!")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới!")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp!")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
