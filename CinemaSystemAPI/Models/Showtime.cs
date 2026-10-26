using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    // Ràng buộc "không chồng chéo thời gian trong cùng phòng" được kiểm tra ở tầng Service.
    [Table("Showtimes")]
    public class Showtime
    {
        public int Id { get; set; }

        public int MovieId { get; set; }

        public int RoomId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime StartTime { get; set; }

        // CHECK > StartTime
        [Column(TypeName = "datetime")]
        public DateTime EndTime { get; set; }

        // CHECK >= 0
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        public virtual Movie Movie { get; set; } = null!;
        public virtual Room Room { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();
    }
}
