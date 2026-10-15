using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(150)")]
        public string Email { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(max)")]
        public string PasswordHash { get; set; } = null!;

        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        // Admin, Staff, Customer
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Role { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
