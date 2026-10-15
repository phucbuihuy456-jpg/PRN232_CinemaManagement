using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    // Composite PK (BookingId, ComboId) - cấu hình trong CinemaDbContext
    [Table("BookingCombos")]
    public class BookingCombo
    {
        public int BookingId { get; set; }

        public int ComboId { get; set; }

        // CHECK > 0
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual Combo Combo { get; set; } = null!;
    }
}
