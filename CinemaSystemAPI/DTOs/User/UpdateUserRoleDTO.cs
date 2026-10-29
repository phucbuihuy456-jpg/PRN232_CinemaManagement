using System.ComponentModel.DataAnnotations;

namespace CinemaSystemAPI.DTOs.User
{
    public class UpdateUserRoleDTO
    {
        [Required(ErrorMessage = "Vai trò không được để trống")]
        public string Role { get; set; } = null!;
    }
}
