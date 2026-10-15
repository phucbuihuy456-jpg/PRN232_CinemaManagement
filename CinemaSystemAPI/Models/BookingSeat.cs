using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    // Ràng buộc "một ghế trong cùng suất chiếu không thể thuộc hai đơn Confirmed"
    // được kiểm tra ở tầng Service (transaction + RowVersion của Booking).
    [Table("BookingSeats")]
    public class BookingSeat
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public int SeatId { get; set; }

        // Price = BasePrice + ExtraPrice
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual Seat Seat { get; set; } = null!;
    }
}
