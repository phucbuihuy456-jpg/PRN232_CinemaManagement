using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    // Unique (RoomId, RowCode, SeatNumber) - cấu hình trong CinemaDbContext
    [Table("Seats")]
    public class Seat
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        [Required]
        [Column(TypeName = "varchar(5)")]
        public string RowCode { get; set; } = null!;

        public int SeatNumber { get; set; }

        public int SeatTypeId { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Room Room { get; set; } = null!;
        public virtual SeatType SeatType { get; set; } = null!;
        public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
        public virtual ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();
    }
}
