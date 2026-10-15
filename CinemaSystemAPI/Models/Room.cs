using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Rooms")]
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        // 2D, 3D, IMAX
        [Column(TypeName = "varchar(20)")]
        public string? ScreenType { get; set; }

        // CHECK > 0
        public int TotalSeats { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}
