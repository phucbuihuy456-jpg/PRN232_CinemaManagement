using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Bookings")]
    public class Booking
    {
        public int Id { get; set; }

        // NULL khi bán tại quầy cho khách vãng lai
        public int? UserId { get; set; }

        public int ShowtimeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Pending, Confirmed, Cancelled, Completed
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        // Unpaid, Paid, Refunded
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string PaymentStatus { get; set; } = "Unpaid";

        // Unique
        [Column(TypeName = "varchar(255)")]
        public string? QrCodeToken { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        // Optimistic concurrency
        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[8];

        public virtual User? User { get; set; }
        public virtual Showtime Showtime { get; set; } = null!;
        public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
        public virtual ICollection<BookingCombo> BookingCombos { get; set; } = new List<BookingCombo>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
