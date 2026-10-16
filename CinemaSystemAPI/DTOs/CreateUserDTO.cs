using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.DTOs
{
    public class CreateUserDTO
    {
        public string FullName { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        [PasswordPropertyText(true)]
        public string PasswordHash { get; set; } = null!;
        [Required]
        public string? PhoneNumber { get; set; }
    }
}
