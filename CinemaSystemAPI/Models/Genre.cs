using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Genres")]
    public class Genre
    {
        public int Id { get; set; }

        // Unique
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }
}
