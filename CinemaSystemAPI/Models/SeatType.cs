using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("SeatTypes")]
    public class SeatType
    {
        public int Id { get; set; }

        // Standard, VIP, Couple
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        // CHECK >= 0
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraPrice { get; set; }

        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
