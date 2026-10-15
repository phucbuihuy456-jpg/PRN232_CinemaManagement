using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Combos")]
    public class Combo
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<BookingCombo> BookingCombos { get; set; } = new List<BookingCombo>();
    }
}
