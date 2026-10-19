using System.ComponentModel.DataAnnotations;

namespace CinemaSystemAPI.DTOs.Combos
{
    public class CreateComboDto
    {
        [Required(ErrorMessage = "Tên combo không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên combo tối đa 100 ký tự")]
        public string Name { get; set; } = null!;

        [MaxLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }

        [Range(0, 100000000, ErrorMessage = "Giá combo phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [MaxLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
